use once_cell::sync::Lazy;
use std::ffi::{c_char, c_void, CStr, CString};
use std::ptr;
use std::sync::Arc;
use tokio::runtime::Runtime;
use zenoh::config::Config;
use zenoh::prelude::*;
use zenoh::publication::Publisher as ZenohPublisher;
use zenoh::sample::Sample as ZenohSample;
use zenoh::Session as ZenohSession;
use zenoh::subscriber::Subscriber as ZenohSubscriber;

// Global Tokio runtime for async operations
static RUNTIME: Lazy<Runtime> = Lazy::new(|| {
    Runtime::new().expect("Failed to create Tokio runtime")
});

/// Opaque handle for Zenoh session
#[repr(C)]
pub struct SessionHandle {
    session: Arc<ZenohSession>,
}

/// Opaque handle for Zenoh publisher
#[repr(C)]
pub struct PublisherHandle {
    publisher: Arc<ZenohPublisher<'static>>,
}

/// Opaque handle for Zenoh subscriber
#[repr(C)]
pub struct SubscriberHandle {
    _subscriber: Arc<ZenohSubscriber<'static>>,
}

/// Sample data structure passed to subscriber callbacks
#[repr(C)]
pub struct ZenohSample {
    pub key_expr: *const c_char,
    pub payload_data: *const u8,
    pub payload_len: usize,
}

/// Callback function type for subscriber
pub type ZenohSubscriberCallback = unsafe extern "C" fn(*const ZenohSample, *mut c_void);

/// Error codes
#[repr(C)]
pub enum ZenohError {
    Ok = 0,
    InvalidConfig = 1,
    SessionClosed = 2,
    InvalidKeyExpr = 3,
    PutFailed = 4,
    NullPointer = 5,
    Unknown = 255,
}

/// Opens a Zenoh session with the given configuration (JSON string).
/// Pass NULL or empty string for default configuration.
/// Returns a pointer to SessionHandle on success, NULL on failure.
#[no_mangle]
pub extern "C" fn zenoh_open(config_json: *const c_char) -> *mut SessionHandle {
    let config = if config_json.is_null() {
        Config::default()
    } else {
        let config_str = unsafe {
            match CStr::from_ptr(config_json).to_str() {
                Ok(s) => s,
                Err(_) => return ptr::null_mut(),
            }
        };

        if config_str.is_empty() {
            Config::default()
        } else {
            match serde_json::from_str::<serde_json::Value>(config_str) {
                Ok(_) => {
                    // For simplicity, use default config for now
                    // Full JSON config parsing can be added later
                    Config::default()
                }
                Err(_) => return ptr::null_mut(),
            }
        }
    };

    // Open session using the global runtime
    let session_result = RUNTIME.block_on(async {
        zenoh::open(config).await
    });

    match session_result {
        Ok(session) => {
            let handle = Box::new(SessionHandle {
                session: Arc::new(session),
            });
            Box::into_raw(handle)
        }
        Err(_) => ptr::null_mut(),
    }
}

/// Closes a Zenoh session and frees all associated resources.
#[no_mangle]
pub extern "C" fn zenoh_close(session: *mut SessionHandle) {
    if session.is_null() {
        return;
    }

    unsafe {
        let handle = Box::from_raw(session);
        // Session will be dropped here, closing the connection
        drop(handle);
    }
}

/// Declares a publisher on the given key expression.
/// Returns a pointer to PublisherHandle on success, NULL on failure.
#[no_mangle]
pub extern "C" fn zenoh_declare_publisher(
    session: *mut SessionHandle,
    key_expr: *const c_char,
) -> *mut PublisherHandle {
    if session.is_null() || key_expr.is_null() {
        return ptr::null_mut();
    }

    let handle = unsafe { &*session };
    let key = unsafe {
        match CStr::from_ptr(key_expr).to_str() {
            Ok(s) => s,
            Err(_) => return ptr::null_mut(),
        }
    };

    let publisher_result = RUNTIME.block_on(async {
        handle.session.declare_publisher(key).await
    });

    match publisher_result {
        Ok(publisher) => {
            // Convert to static lifetime by leaking the session Arc
            let static_publisher: ZenohPublisher<'static> = unsafe {
                std::mem::transmute(publisher)
            };

            let pub_handle = Box::new(PublisherHandle {
                publisher: Arc::new(static_publisher),
            });
            Box::into_raw(pub_handle)
        }
        Err(_) => ptr::null_mut(),
    }
}

/// Publishes data on the given publisher.
/// Returns ZenohError code.
#[no_mangle]
pub extern "C" fn zenoh_publisher_put(
    publisher: *mut PublisherHandle,
    payload: *const u8,
    payload_len: usize,
) -> ZenohError {
    if publisher.is_null() || payload.is_null() {
        return ZenohError::NullPointer;
    }

    let handle = unsafe { &*publisher };
    let data = unsafe { std::slice::from_raw_parts(payload, payload_len) };

    let result = RUNTIME.block_on(async {
        handle.publisher.put(data.to_vec()).await
    });

    match result {
        Ok(_) => ZenohError::Ok,
        Err(_) => ZenohError::PutFailed,
    }
}

/// Undeclares and frees a publisher.
#[no_mangle]
pub extern "C" fn zenoh_undeclare_publisher(publisher: *mut PublisherHandle) {
    if publisher.is_null() {
        return;
    }

    unsafe {
        let handle = Box::from_raw(publisher);
        // Publisher will be undeclared when dropped
        drop(handle);
    }
}

/// Declares a subscriber on the given key expression with a callback.
/// Returns a pointer to SubscriberHandle on success, NULL on failure.
#[no_mangle]
pub extern "C" fn zenoh_declare_subscriber(
    session: *mut SessionHandle,
    key_expr: *const c_char,
    callback: ZenohSubscriberCallback,
    context: *mut c_void,
) -> *mut SubscriberHandle {
    if session.is_null() || key_expr.is_null() {
        return ptr::null_mut();
    }

    let handle = unsafe { &*session };
    let key = unsafe {
        match CStr::from_ptr(key_expr).to_str() {
            Ok(s) => s,
            Err(_) => return ptr::null_mut(),
        }
    };

    let subscriber_result = RUNTIME.block_on(async {
        handle.session
            .declare_subscriber(key)
            .callback(move |sample: ZenohSample| {
                // Convert key expression to C string
                let key_cstr = match CString::new(sample.key_expr().as_str()) {
                    Ok(s) => s,
                    Err(_) => return,
                };

                // Get payload
                let payload = sample.payload().to_bytes();

                let c_sample = ZenohSample {
                    key_expr: key_cstr.as_ptr(),
                    payload_data: payload.as_ptr(),
                    payload_len: payload.len(),
                };

                // Call C# callback
                unsafe {
                    callback(&c_sample, context);
                }

                // Keep the CString alive until callback returns
                drop(key_cstr);
            })
            .await
    });

    match subscriber_result {
        Ok(subscriber) => {
            // Convert to static lifetime
            let static_subscriber: ZenohSubscriber<'static> = unsafe {
                std::mem::transmute(subscriber)
            };

            let sub_handle = Box::new(SubscriberHandle {
                _subscriber: Arc::new(static_subscriber),
            });
            Box::into_raw(sub_handle)
        }
        Err(_) => ptr::null_mut(),
    }
}

/// Undeclares and frees a subscriber.
#[no_mangle]
pub extern "C" fn zenoh_undeclare_subscriber(subscriber: *mut SubscriberHandle) {
    if subscriber.is_null() {
        return;
    }

    unsafe {
        let handle = Box::from_raw(subscriber);
        // Subscriber will be undeclared when dropped
        drop(handle);
    }
}

/// Gets the last error message (for debugging).
/// Returns a pointer to a static error string.
#[no_mangle]
pub extern "C" fn zenoh_get_error_message() -> *const c_char {
    static ERROR_MSG: &str = "Check logs for detailed error information\0";
    ERROR_MSG.as_ptr() as *const c_char
}

/// Frees a string allocated by the Zenoh FFI.
#[no_mangle]
pub extern "C" fn zenoh_free_string(s: *mut c_char) {
    if s.is_null() {
        return;
    }
    unsafe {
        let _ = CString::from_raw(s);
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_session_lifecycle() {
        let session = zenoh_open(ptr::null());
        assert!(!session.is_null());
        zenoh_close(session);
    }

    #[test]
    fn test_publisher_lifecycle() {
        let session = zenoh_open(ptr::null());
        assert!(!session.is_null());

        let key = CString::new("test/key").unwrap();
        let publisher = zenoh_declare_publisher(session, key.as_ptr());
        assert!(!publisher.is_null());

        let data = b"test data";
        let result = zenoh_publisher_put(publisher, data.as_ptr(), data.len());
        assert!(matches!(result, ZenohError::Ok));

        zenoh_undeclare_publisher(publisher);
        zenoh_close(session);
    }

    #[test]
    fn test_subscriber_lifecycle() {
        let session = zenoh_open(ptr::null());
        assert!(!session.is_null());

        extern "C" fn test_callback(_sample: *const ZenohSample, _context: *mut c_void) {
            // Callback for testing
        }

        let key = CString::new("test/key").unwrap();
        let subscriber = zenoh_declare_subscriber(
            session,
            key.as_ptr(),
            test_callback,
            ptr::null_mut(),
        );
        assert!(!subscriber.is_null());

        zenoh_undeclare_subscriber(subscriber);
        zenoh_close(session);
    }
}
