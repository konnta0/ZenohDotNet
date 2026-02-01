use once_cell::sync::Lazy;
use std::cell::RefCell;
use std::ffi::{c_char, c_void, CStr, CString};
use std::panic;
use std::ptr;
use std::sync::Arc;
use tokio::runtime::Runtime;
use zenoh::config::Config;
use zenoh::pubsub::{Publisher, Subscriber};
use zenoh::qos::{CongestionControl, Priority};
use zenoh::query::{Query, Queryable};
use zenoh::sample::{Sample, SampleKind};
use zenoh::Session;
use zenoh::liveliness::LivelinessToken;

// Global Tokio runtime for async operations
static RUNTIME: Lazy<Runtime> = Lazy::new(|| {
    Runtime::new().expect("Failed to create Tokio runtime")
});

/// Runs an async block, handling the case where we're already inside a runtime.
/// This prevents panic/deadlock when C# calls FFI from within a callback.
/// 
/// Note: This is for simple async operations without callbacks.
/// For operations with callbacks that need to execute in the runtime context,
/// use RUNTIME.block_on directly since the callback will run on the runtime thread.
fn run_blocking<F, T>(f: F) -> T
where
    F: std::future::Future<Output = T> + Send + 'static,
    T: Send + 'static,
{
    use tokio::runtime::Handle;
    
    if Handle::try_current().is_ok() {
        // Already inside a runtime - spawn on a separate thread to avoid deadlock
        let handle = RUNTIME.handle().clone();
        let (tx, rx) = std::sync::mpsc::channel();
        std::thread::spawn(move || {
            let result = handle.block_on(f);
            let _ = tx.send(result);
        });
        rx.recv().expect("Failed to receive result from spawned thread")
    } else {
        // Not inside a runtime - safe to block directly
        RUNTIME.block_on(f)
    }
}

/// Variant of run_blocking for operations that don't need 'static lifetime.
/// This is safe because we wait for completion before returning.
fn run_blocking_local<F, T>(f: F) -> T
where
    F: std::future::Future<Output = T> + Send,
    T: Send,
{
    use tokio::runtime::Handle;
    
    if Handle::try_current().is_ok() {
        // Already inside a runtime - we need to spawn blocking to avoid deadlock
        // Use scoped thread to allow non-'static futures
        let (tx, rx) = std::sync::mpsc::channel();
        std::thread::scope(|s| {
            s.spawn(|| {
                let result = RUNTIME.block_on(f);
                let _ = tx.send(result);
            });
        });
        rx.recv().expect("Failed to receive result from spawned thread")
    } else {
        // Not inside a runtime - safe to block directly
        RUNTIME.block_on(f)
    }
}

// ============== Error Handling ==============

thread_local! {
    static LAST_ERROR: RefCell<Option<CString>> = const { RefCell::new(None) };
}

fn set_error(msg: impl ToString) {
    LAST_ERROR.with(|e| {
        *e.borrow_mut() = CString::new(msg.to_string()).ok();
    });
}

fn clear_error() {
    LAST_ERROR.with(|e| {
        *e.borrow_mut() = None;
    });
}

/// Gets the last error message.
/// Returns NULL if no error occurred.
/// The returned string is valid until the next FFI call on the same thread.
#[no_mangle]
pub extern "C" fn zenoh_last_error() -> *const c_char {
    LAST_ERROR.with(|e| {
        match e.borrow().as_ref() {
            Some(s) => s.as_ptr(),
            None => ptr::null(),
        }
    })
}

// ============== Internal Wrapper Types ==============

struct SessionWrapper {
    session: Arc<Session>,
}

struct PublisherWrapper {
    publisher: Arc<Publisher<'static>>,
    /// Holds a reference to the session to ensure it outlives the publisher.
    /// This prevents undefined behavior from the transmute to 'static.
    _session: Arc<Session>,
}

struct SubscriberWrapper {
    _subscriber: Arc<Subscriber<()>>,
}

struct QueryableWrapper {
    _queryable: Arc<Queryable<()>>,
}

struct QueryWrapper {
    query: Box<Query>,
}

struct LivelinessTokenWrapper {
    _token: LivelinessToken,
}

struct QuerierWrapper {
    querier: zenoh::query::Querier<'static>,
    /// Holds a reference to the session to ensure it outlives the querier.
    /// This prevents undefined behavior from the transmute to 'static.
    _session: Arc<Session>,
}

// ============== QoS Types ==============

/// Congestion control strategy
#[repr(C)]
#[derive(Clone, Copy)]
pub enum ZenohCongestionControl {
    /// Block if the buffer is full
    Block = 0,
    /// Drop the message if the buffer is full
    Drop = 1,
}

/// Priority of messages
#[repr(C)]
#[derive(Clone, Copy)]
pub enum ZenohPriority {
    RealTime = 1,
    InteractiveHigh = 2,
    InteractiveLow = 3,
    DataHigh = 4,
    Data = 5,
    DataLow = 6,
    Background = 7,
}

/// Sample kind (Put or Delete)
#[repr(C)]
#[derive(Clone, Copy)]
pub enum ZenohSampleKind {
    Put = 0,
    Delete = 1,
}

/// Publisher options
#[repr(C)]
#[derive(Copy, Clone)]
pub struct PublisherOptions {
    pub congestion_control: ZenohCongestionControl,
    pub priority: ZenohPriority,
    pub is_express: bool,
}

/// Encoding ID for payload
#[repr(C)]
#[derive(Clone, Copy)]
pub enum ZenohEncodingId {
    Empty = 0,
    AppOctetStream = 1,
    TextPlain = 2,
    AppJson = 3,
    TextJson = 4,
    AppCbor = 5,
    AppYaml = 6,
    TextYaml = 7,
    TextXml = 8,
    AppXml = 9,
    TextCsv = 10,
    AppProtobuf = 11,
    TextHtml = 12,
}

/// Timestamp structure
#[repr(C)]
#[derive(Copy, Clone)]
pub struct ZenohTimestamp {
    /// NTP64 timestamp (seconds since epoch in upper 32 bits, fraction in lower 32 bits)
    pub time_ntp64: u64,
    /// Unique ID of the timestamp source (first 16 bytes of ZenohId)
    pub id: [u8; 16],
}

/// Attachment key-value pair
#[repr(C)]
pub struct ZenohAttachmentItem {
    pub key: *const c_char,
    pub value: *const u8,
    pub value_len: usize,
}

/// Sample data structure passed to subscriber callbacks
#[repr(C)]
pub struct SampleData {
    pub key_expr: *const c_char,
    pub payload_data: *const u8,
    pub payload_len: usize,
    pub kind: ZenohSampleKind,
    pub encoding_id: ZenohEncodingId,
    pub timestamp_valid: bool,
    pub timestamp: ZenohTimestamp,
}

/// Callback function type for subscriber
pub type ZenohSubscriberCallback = unsafe extern "C" fn(*const SampleData, *mut c_void);

/// Callback function type for queryable
pub type ZenohQueryableCallback = unsafe extern "C" fn(*mut c_void, *mut c_void);

/// Callback function type for get (query replies)
pub type ZenohGetCallback = unsafe extern "C" fn(*const SampleData, *mut c_void);

/// Error codes
#[repr(C)]
pub enum ZenohError {
    Ok = 0,
    InvalidConfig = 1,
    SessionClosed = 2,
    InvalidKeyExpr = 3,
    PutFailed = 4,
    NullPointer = 5,
    Panic = 254,
    Unknown = 255,
}

/// Opens a Zenoh session with the given configuration (JSON5 string).
/// Pass NULL or empty string for default configuration.
/// Returns a pointer on success, NULL on failure.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_open(config_json: *const c_char) -> *mut c_void {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        let config = if config_json.is_null() {
            Config::default()
        } else {
            let config_str = unsafe {
                match CStr::from_ptr(config_json).to_str() {
                    Ok(s) => s,
                    Err(e) => {
                        set_error(format!("Invalid UTF-8 in config: {}", e));
                        return ptr::null_mut();
                    }
                }
            };

            if config_str.is_empty() {
                Config::default()
            } else {
                match Config::from_json5(config_str) {
                    Ok(c) => c,
                    Err(e) => {
                        set_error(format!("Config parse error: {}", e));
                        return ptr::null_mut();
                    }
                }
            }
        };

        let session_result = run_blocking(async move {
            zenoh::open(config).await
        });

        match session_result {
            Ok(session) => {
                let handle = Box::new(SessionWrapper {
                    session: Arc::new(session),
                });
                Box::into_raw(handle) as *mut c_void
            }
            Err(e) => {
                set_error(format!("Failed to open session: {}", e));
                ptr::null_mut()
            }
        }
    });
    
    match result {
        Ok(ptr) => ptr,
        Err(_) => {
            set_error("Panic occurred in zenoh_open");
            ptr::null_mut()
        }
    }
}

/// Closes a Zenoh session and frees all associated resources.
#[no_mangle]
pub extern "C" fn zenoh_close(session: *mut c_void) {
    if session.is_null() {
        return;
    }
    let _ = panic::catch_unwind(|| {
        unsafe {
            let _ = Box::from_raw(session as *mut SessionWrapper);
        }
    });
}

/// Declares a publisher on the given key expression.
/// Returns a pointer on success, NULL on failure.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_declare_publisher(
    session: *mut c_void,
    key_expr: *const c_char,
) -> *mut c_void {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if session.is_null() {
            set_error("Session pointer is null");
            return ptr::null_mut();
        }
        if key_expr.is_null() {
            set_error("Key expression is null");
            return ptr::null_mut();
        }

        let handle = unsafe { &*(session as *const SessionWrapper) };
        let key = unsafe {
            match CStr::from_ptr(key_expr).to_str() {
                Ok(s) => s,
                Err(e) => {
                    set_error(format!("Invalid UTF-8 in key expression: {}", e));
                    return ptr::null_mut();
                }
            }
        };

        let session_arc = handle.session.clone();
        let publisher_result = run_blocking(async move {
            handle.session.declare_publisher(key).await
        });

        match publisher_result {
            Ok(publisher) => {
                let static_publisher: Publisher<'static> = unsafe {
                    std::mem::transmute(publisher)
                };
                let pub_handle = Box::new(PublisherWrapper {
                    publisher: Arc::new(static_publisher),
                    _session: session_arc,
                });
                Box::into_raw(pub_handle) as *mut c_void
            }
            Err(e) => {
                set_error(format!("Failed to declare publisher: {}", e));
                ptr::null_mut()
            }
        }
    });
    
    match result {
        Ok(ptr) => ptr,
        Err(_) => {
            set_error("Panic occurred in zenoh_declare_publisher");
            ptr::null_mut()
        }
    }
}

/// Publishes data on the given publisher.
/// Returns ZenohError code.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_publisher_put(
    publisher: *mut c_void,
    payload: *const u8,
    payload_len: usize,
) -> ZenohError {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if publisher.is_null() {
            set_error("Publisher pointer is null");
            return ZenohError::NullPointer;
        }

        // Allow empty payload (payload can be null if len is 0)
        if payload.is_null() && payload_len > 0 {
            set_error("Payload pointer is null but length > 0");
            return ZenohError::NullPointer;
        }

        let handle = unsafe { &*(publisher as *const PublisherWrapper) };
        let data = if payload.is_null() || payload_len == 0 {
            Vec::new()
        } else {
            unsafe { std::slice::from_raw_parts(payload, payload_len) }.to_vec()
        };

        let put_result = run_blocking(async move {
            handle.publisher.put(data).await
        });

        match put_result {
            Ok(_) => ZenohError::Ok,
            Err(e) => {
                set_error(format!("Put failed: {}", e));
                ZenohError::PutFailed
            }
        }
    });
    
    match result {
        Ok(err) => err,
        Err(_) => {
            set_error("Panic occurred in zenoh_publisher_put");
            ZenohError::Panic
        }
    }
}

/// Undeclares and frees a publisher.
#[no_mangle]
pub extern "C" fn zenoh_undeclare_publisher(publisher: *mut c_void) {
    if publisher.is_null() {
        return;
    }
    let _ = panic::catch_unwind(|| {
        unsafe {
            let _ = Box::from_raw(publisher as *mut PublisherWrapper);
        }
    });
}

/// Declares a subscriber on the given key expression with a callback.
/// Returns a pointer on success, NULL on failure.
/// 
/// # Safety
/// The SampleData pointer passed to the callback is valid only during the callback invocation.
/// Do not store this pointer or its contents (key_expr, payload_data) for later use.
/// Copy the data if you need to retain it.
/// 
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_declare_subscriber(
    session: *mut c_void,
    key_expr: *const c_char,
    callback: ZenohSubscriberCallback,
    context: *mut c_void,
) -> *mut c_void {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if session.is_null() {
            set_error("Session pointer is null");
            return ptr::null_mut();
        }
        if key_expr.is_null() {
            set_error("Key expression is null");
            return ptr::null_mut();
        }

        let handle = unsafe { &*(session as *const SessionWrapper) };
        let key = unsafe {
            match CStr::from_ptr(key_expr).to_str() {
                Ok(s) => s,
                Err(e) => {
                    set_error(format!("Invalid UTF-8 in key expression: {}", e));
                    return ptr::null_mut();
                }
            }
        };

        let context_ptr = context as usize;

        let subscriber_result = run_blocking_local(async {
            handle.session
                .declare_subscriber(key)
                .callback(move |sample: Sample| {
                    let key_cstr = match CString::new(sample.key_expr().as_str()) {
                        Ok(s) => s,
                        Err(_) => return,
                    };

                    let payload = sample.payload().to_bytes();
                    let kind = match sample.kind() {
                        SampleKind::Put => ZenohSampleKind::Put,
                        SampleKind::Delete => ZenohSampleKind::Delete,
                    };

                    // Get encoding
                    let encoding_id = encoding_to_id(sample.encoding());

                    // Get timestamp
                    let (timestamp_valid, timestamp) = match sample.timestamp() {
                        Some(ts) => {
                            let ntp = ts.get_time().as_u64();
                            let id_bytes = ts.get_id().to_le_bytes();
                            let mut id = [0u8; 16];
                            id.copy_from_slice(&id_bytes[..16.min(id_bytes.len())]);
                            (true, ZenohTimestamp { time_ntp64: ntp, id })
                        }
                        None => (false, ZenohTimestamp { time_ntp64: 0, id: [0u8; 16] }),
                    };

                    let c_sample = SampleData {
                        key_expr: key_cstr.as_ptr(),
                        payload_data: payload.as_ptr(),
                        payload_len: payload.len(),
                        kind,
                        encoding_id,
                        timestamp_valid,
                        timestamp,
                    };

                    unsafe {
                        callback(&c_sample, context_ptr as *mut c_void);
                    }
                })
                .await
        });

        match subscriber_result {
            Ok(subscriber) => {
                let sub_handle = Box::new(SubscriberWrapper {
                    _subscriber: Arc::new(subscriber),
                });
                Box::into_raw(sub_handle) as *mut c_void
            }
            Err(e) => {
                set_error(format!("Failed to declare subscriber: {}", e));
                ptr::null_mut()
            }
        }
    });
    
    match result {
        Ok(ptr) => ptr,
        Err(_) => {
            set_error("Panic occurred in zenoh_declare_subscriber");
            ptr::null_mut()
        }
    }
}

/// Undeclares and frees a subscriber.
#[no_mangle]
pub extern "C" fn zenoh_undeclare_subscriber(subscriber: *mut c_void) {
    if subscriber.is_null() {
        return;
    }
    let _ = panic::catch_unwind(|| {
        unsafe {
            let _ = Box::from_raw(subscriber as *mut SubscriberWrapper);
        }
    });
}

/// Frees a string allocated by the Zenoh FFI.
#[no_mangle]
pub extern "C" fn zenoh_free_string(s: *mut c_char) {
    if s.is_null() {
        return;
    }
    let _ = panic::catch_unwind(|| {
        unsafe {
            let _ = CString::from_raw(s);
        }
    });
}

/// Performs a get query (request-response pattern).
/// Returns 0 on success, error code on failure.
/// 
/// # Safety
/// The SampleData pointer passed to the callback is valid only during the callback invocation.
/// Do not store this pointer or its contents (key_expr, payload_data) for later use.
/// Copy the data if you need to retain it.
/// 
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_get(
    session: *mut c_void,
    selector: *const c_char,
    callback: ZenohGetCallback,
    context: *mut c_void,
) -> ZenohError {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if session.is_null() {
            set_error("Session pointer is null");
            return ZenohError::NullPointer;
        }
        if selector.is_null() {
            set_error("Selector is null");
            return ZenohError::NullPointer;
        }

        let handle = unsafe { &*(session as *const SessionWrapper) };
        let selector_str = unsafe {
            match CStr::from_ptr(selector).to_str() {
                Ok(s) => s,
                Err(e) => {
                    set_error(format!("Invalid UTF-8 in selector: {}", e));
                    return ZenohError::InvalidKeyExpr;
                }
            }
        };

        let context_ptr = context as usize;

        let query_result = run_blocking_local(async {
            let replies = handle.session.get(selector_str).await;

            match replies {
                Ok(reply_receiver) => {
                    while let Ok(reply) = reply_receiver.recv_async().await {
                        if let Ok(sample) = reply.result() {
                            let key_cstr = match CString::new(sample.key_expr().as_str()) {
                                Ok(s) => s,
                                Err(_) => continue,
                            };

                            let payload = sample.payload().to_bytes();
                            let kind = match sample.kind() {
                                SampleKind::Put => ZenohSampleKind::Put,
                                SampleKind::Delete => ZenohSampleKind::Delete,
                            };

                            let encoding_id = encoding_to_id(sample.encoding());
                            let (timestamp_valid, timestamp) = match sample.timestamp() {
                                Some(ts) => {
                                    let ntp = ts.get_time().as_u64();
                                    let id_bytes = ts.get_id().to_le_bytes();
                                    let mut id = [0u8; 16];
                                    id.copy_from_slice(&id_bytes[..16.min(id_bytes.len())]);
                                    (true, ZenohTimestamp { time_ntp64: ntp, id })
                                }
                                None => (false, ZenohTimestamp { time_ntp64: 0, id: [0u8; 16] }),
                            };

                            let c_sample = SampleData {
                                key_expr: key_cstr.as_ptr(),
                                payload_data: payload.as_ptr(),
                                payload_len: payload.len(),
                                kind,
                                encoding_id,
                                timestamp_valid,
                                timestamp,
                            };

                            unsafe {
                                callback(&c_sample, context_ptr as *mut c_void);
                            }
                        }
                    }
                    ZenohError::Ok
                }
                Err(e) => {
                    set_error(format!("Get query failed: {}", e));
                    ZenohError::Unknown
                }
            }
        });

        query_result
    });

    match result {
        Ok(err) => err,
        Err(_) => {
            set_error("Panic occurred in zenoh_get");
            ZenohError::Panic
        }
    }
}

/// Declares a queryable that responds to get queries.
/// Returns a pointer on success, NULL on failure.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_declare_queryable(
    session: *mut c_void,
    key_expr: *const c_char,
    callback: ZenohQueryableCallback,
    context: *mut c_void,
) -> *mut c_void {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if session.is_null() {
            set_error("Session pointer is null");
            return ptr::null_mut();
        }
        if key_expr.is_null() {
            set_error("Key expression is null");
            return ptr::null_mut();
        }

        let handle = unsafe { &*(session as *const SessionWrapper) };
        let key = unsafe {
            match CStr::from_ptr(key_expr).to_str() {
                Ok(s) => s,
                Err(e) => {
                    set_error(format!("Invalid UTF-8 in key expression: {}", e));
                    return ptr::null_mut();
                }
            }
        };

        let context_ptr = context as usize;

        let queryable_result = run_blocking_local(async {
            handle.session
                .declare_queryable(key)
                .callback(move |query: Query| {
                    let query_handle = Box::new(QueryWrapper {
                        query: Box::new(query),
                    });
                    let query_ptr = Box::into_raw(query_handle) as *mut c_void;

                    unsafe {
                        callback(query_ptr, context_ptr as *mut c_void);
                    }
                })
                .await
        });

        match queryable_result {
            Ok(queryable) => {
                let handle = Box::new(QueryableWrapper {
                    _queryable: Arc::new(queryable),
                });
                Box::into_raw(handle) as *mut c_void
            }
            Err(e) => {
                set_error(format!("Failed to declare queryable: {}", e));
                ptr::null_mut()
            }
        }
    });
    
    match result {
        Ok(ptr) => ptr,
        Err(_) => {
            set_error("Panic occurred in zenoh_declare_queryable");
            ptr::null_mut()
        }
    }
}

/// Replies to a query with data.
/// The query handle is consumed by this operation.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_query_reply(
    query: *mut c_void,
    key_expr: *const c_char,
    payload: *const u8,
    payload_len: usize,
) -> ZenohError {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if query.is_null() {
            set_error("Query pointer is null");
            return ZenohError::NullPointer;
        }
        if key_expr.is_null() {
            set_error("Key expression is null");
            return ZenohError::NullPointer;
        }
        if payload.is_null() {
            set_error("Payload pointer is null");
            return ZenohError::NullPointer;
        }

        let query_handle = unsafe { Box::from_raw(query as *mut QueryWrapper) };
        let key = unsafe {
            match CStr::from_ptr(key_expr).to_str() {
                Ok(s) => s.to_string(),
                Err(e) => {
                    set_error(format!("Invalid UTF-8 in key expression: {}", e));
                    return ZenohError::InvalidKeyExpr;
                }
            }
        };
        let data = unsafe { std::slice::from_raw_parts(payload, payload_len) }.to_vec();

        let reply_result = run_blocking(async move {
            query_handle.query.reply(key, data).await
        });
        match reply_result {
            Ok(_) => ZenohError::Ok,
            Err(e) => {
                set_error(format!("Query reply failed: {}", e));
                ZenohError::Unknown
            }
        }
    });
    
    match result {
        Ok(err) => err,
        Err(_) => {
            set_error("Panic occurred in zenoh_query_reply");
            ZenohError::Panic
        }
    }
}

/// Drops (frees) a query without replying.
/// Use this when you receive a query but decide not to reply to it.
/// This prevents memory leaks when queries are not replied to.
#[no_mangle]
pub extern "C" fn zenoh_query_drop(query: *mut c_void) {
    if query.is_null() {
        return;
    }
    let _ = panic::catch_unwind(|| {
        unsafe {
            let _ = Box::from_raw(query as *mut QueryWrapper);
        }
    });
}

/// Gets the selector (key expression) of a query.
/// Returns a C string that must be freed with zenoh_free_string.
#[no_mangle]
pub extern "C" fn zenoh_query_selector(query: *const c_void) -> *mut c_char {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if query.is_null() {
            set_error("Query pointer is null");
            return ptr::null_mut();
        }

        let handle = unsafe { &*(query as *const QueryWrapper) };
        let selector = handle.query.selector();
        let key_expr_str = selector.key_expr().as_str();

        match CString::new(key_expr_str) {
            Ok(cstr) => cstr.into_raw(),
            Err(e) => {
                set_error(format!("Invalid string: {}", e));
                ptr::null_mut()
            }
        }
    });
    
    match result {
        Ok(ptr) => ptr,
        Err(_) => {
            set_error("Panic occurred in zenoh_query_selector");
            ptr::null_mut()
        }
    }
}

/// Undeclares and frees a queryable.
#[no_mangle]
pub extern "C" fn zenoh_undeclare_queryable(queryable: *mut c_void) {
    if queryable.is_null() {
        return;
    }
    let _ = panic::catch_unwind(|| {
        unsafe {
            let _ = Box::from_raw(queryable as *mut QueryableWrapper);
        }
    });
}

// ============== Publisher with Options ==============

/// Creates default publisher options
#[no_mangle]
pub extern "C" fn zenoh_publisher_options_default() -> PublisherOptions {
    PublisherOptions {
        congestion_control: ZenohCongestionControl::Drop,
        priority: ZenohPriority::Data,
        is_express: false,
    }
}

/// Declares a publisher with options.
/// Returns a pointer on success, NULL on failure.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_declare_publisher_with_options(
    session: *mut c_void,
    key_expr: *const c_char,
    options: *const PublisherOptions,
) -> *mut c_void {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if session.is_null() {
            set_error("Session pointer is null");
            return ptr::null_mut();
        }
        if key_expr.is_null() {
            set_error("Key expression is null");
            return ptr::null_mut();
        }

        let handle = unsafe { &*(session as *const SessionWrapper) };
        let key = unsafe {
            match CStr::from_ptr(key_expr).to_str() {
                Ok(s) => s,
                Err(e) => {
                    set_error(format!("Invalid UTF-8 in key expression: {}", e));
                    return ptr::null_mut();
                }
            }
        };

        let opts = if options.is_null() {
            zenoh_publisher_options_default()
        } else {
            unsafe { *options }
        };

        let congestion_control = match opts.congestion_control {
            ZenohCongestionControl::Block => CongestionControl::Block,
            ZenohCongestionControl::Drop => CongestionControl::Drop,
        };

        let priority = match opts.priority {
            ZenohPriority::RealTime => Priority::RealTime,
            ZenohPriority::InteractiveHigh => Priority::InteractiveHigh,
            ZenohPriority::InteractiveLow => Priority::InteractiveLow,
            ZenohPriority::DataHigh => Priority::DataHigh,
            ZenohPriority::Data => Priority::Data,
            ZenohPriority::DataLow => Priority::DataLow,
            ZenohPriority::Background => Priority::Background,
        };

        let session_arc = handle.session.clone();
        let publisher_result = run_blocking(async move {
            handle.session
                .declare_publisher(key)
                .congestion_control(congestion_control)
                .priority(priority)
                .express(opts.is_express)
                .await
        });

        match publisher_result {
            Ok(publisher) => {
                let static_publisher: Publisher<'static> = unsafe {
                    std::mem::transmute(publisher)
                };
                let pub_handle = Box::new(PublisherWrapper {
                    publisher: Arc::new(static_publisher),
                    _session: session_arc,
                });
                Box::into_raw(pub_handle) as *mut c_void
            }
            Err(e) => {
                set_error(format!("Failed to declare publisher: {}", e));
                ptr::null_mut()
            }
        }
    });
    
    match result {
        Ok(ptr) => ptr,
        Err(_) => {
            set_error("Panic occurred in zenoh_declare_publisher_with_options");
            ptr::null_mut()
        }
    }
}

// ============== Delete Operation ==============

/// Deletes data for a key expression.
/// Returns ZenohError code.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_delete(
    session: *mut c_void,
    key_expr: *const c_char,
) -> ZenohError {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if session.is_null() {
            set_error("Session pointer is null");
            return ZenohError::NullPointer;
        }
        if key_expr.is_null() {
            set_error("Key expression is null");
            return ZenohError::NullPointer;
        }

        let handle = unsafe { &*(session as *const SessionWrapper) };
        let key = unsafe {
            match CStr::from_ptr(key_expr).to_str() {
                Ok(s) => s,
                Err(e) => {
                    set_error(format!("Invalid UTF-8 in key expression: {}", e));
                    return ZenohError::InvalidKeyExpr;
                }
            }
        };

        let delete_result = run_blocking(async move {
            handle.session.delete(key).await
        });

        match delete_result {
            Ok(_) => ZenohError::Ok,
            Err(e) => {
                set_error(format!("Delete failed: {}", e));
                ZenohError::Unknown
            }
        }
    });
    
    match result {
        Ok(err) => err,
        Err(_) => {
            set_error("Panic occurred in zenoh_delete");
            ZenohError::Panic
        }
    }
}

/// Deletes data using a publisher.
/// Returns ZenohError code.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_publisher_delete(publisher: *mut c_void) -> ZenohError {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if publisher.is_null() {
            set_error("Publisher pointer is null");
            return ZenohError::NullPointer;
        }

        let handle = unsafe { &*(publisher as *const PublisherWrapper) };

        let delete_result = run_blocking(async move {
            handle.publisher.delete().await
        });

        match delete_result {
            Ok(_) => ZenohError::Ok,
            Err(e) => {
                set_error(format!("Publisher delete failed: {}", e));
                ZenohError::Unknown
            }
        }
    });
    
    match result {
        Ok(err) => err,
        Err(_) => {
            set_error("Panic occurred in zenoh_publisher_delete");
            ZenohError::Panic
        }
    }
}

// ============== Put with Options ==============

/// Put data directly on a session (without declaring a publisher).
/// Returns ZenohError code.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_put(
    session: *mut c_void,
    key_expr: *const c_char,
    payload: *const u8,
    payload_len: usize,
) -> ZenohError {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if session.is_null() {
            set_error("Session pointer is null");
            return ZenohError::NullPointer;
        }
        if key_expr.is_null() {
            set_error("Key expression is null");
            return ZenohError::NullPointer;
        }

        let handle = unsafe { &*(session as *const SessionWrapper) };
        let key = unsafe {
            match CStr::from_ptr(key_expr).to_str() {
                Ok(s) => s,
                Err(e) => {
                    set_error(format!("Invalid UTF-8 in key expression: {}", e));
                    return ZenohError::InvalidKeyExpr;
                }
            }
        };

        let data = if payload.is_null() || payload_len == 0 {
            Vec::new()
        } else {
            unsafe { std::slice::from_raw_parts(payload, payload_len) }.to_vec()
        };

        let put_result = run_blocking(async move {
            handle.session.put(key, data).await
        });

        match put_result {
            Ok(_) => ZenohError::Ok,
            Err(e) => {
                set_error(format!("Put failed: {}", e));
                ZenohError::PutFailed
            }
        }
    });
    
    match result {
        Ok(err) => err,
        Err(_) => {
            set_error("Panic occurred in zenoh_put");
            ZenohError::Panic
        }
    }
}

// ============== Liveliness ==============

/// Declares a liveliness token for the given key expression.
/// Returns a pointer on success, NULL on failure.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_liveliness_declare_token(
    session: *mut c_void,
    key_expr: *const c_char,
) -> *mut c_void {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if session.is_null() {
            set_error("Session pointer is null");
            return ptr::null_mut();
        }
        if key_expr.is_null() {
            set_error("Key expression is null");
            return ptr::null_mut();
        }

        let handle = unsafe { &*(session as *const SessionWrapper) };
        let key = unsafe {
            match CStr::from_ptr(key_expr).to_str() {
                Ok(s) => s,
                Err(e) => {
                    set_error(format!("Invalid UTF-8 in key expression: {}", e));
                    return ptr::null_mut();
                }
            }
        };

        let token_result = run_blocking(async move {
            handle.session.liveliness().declare_token(key).await
        });

        match token_result {
            Ok(token) => {
                let token_handle = Box::new(LivelinessTokenWrapper { _token: token });
                Box::into_raw(token_handle) as *mut c_void
            }
            Err(e) => {
                set_error(format!("Failed to declare liveliness token: {}", e));
                ptr::null_mut()
            }
        }
    });
    
    match result {
        Ok(ptr) => ptr,
        Err(_) => {
            set_error("Panic occurred in zenoh_liveliness_declare_token");
            ptr::null_mut()
        }
    }
}

/// Undeclares and frees a liveliness token.
#[no_mangle]
pub extern "C" fn zenoh_liveliness_undeclare_token(token: *mut c_void) {
    if token.is_null() {
        return;
    }
    let _ = panic::catch_unwind(|| {
        unsafe {
            let _ = Box::from_raw(token as *mut LivelinessTokenWrapper);
        }
    });
}

/// Callback function type for liveliness subscriber
pub type ZenohLivelinessCallback = unsafe extern "C" fn(*const c_char, bool, *mut c_void);

/// Declares a liveliness subscriber.
/// The callback receives (key_expr, is_alive, context).
/// Returns a pointer on success, NULL on failure.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_liveliness_declare_subscriber(
    session: *mut c_void,
    key_expr: *const c_char,
    callback: ZenohLivelinessCallback,
    context: *mut c_void,
) -> *mut c_void {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if session.is_null() {
            set_error("Session pointer is null");
            return ptr::null_mut();
        }
        if key_expr.is_null() {
            set_error("Key expression is null");
            return ptr::null_mut();
        }

        let handle = unsafe { &*(session as *const SessionWrapper) };
        let key = unsafe {
            match CStr::from_ptr(key_expr).to_str() {
                Ok(s) => s,
                Err(e) => {
                    set_error(format!("Invalid UTF-8 in key expression: {}", e));
                    return ptr::null_mut();
                }
            }
        };

        let context_ptr = context as usize;

        let subscriber_result = run_blocking_local(async {
            handle.session
                .liveliness()
                .declare_subscriber(key)
                .callback(move |sample: Sample| {
                    let key_cstr = match CString::new(sample.key_expr().as_str()) {
                        Ok(s) => s,
                        Err(_) => return,
                    };

                    let is_alive = matches!(sample.kind(), SampleKind::Put);

                    unsafe {
                        callback(key_cstr.as_ptr(), is_alive, context_ptr as *mut c_void);
                    }
                })
                .await
        });

        match subscriber_result {
            Ok(subscriber) => {
                let sub_handle = Box::new(SubscriberWrapper {
                    _subscriber: Arc::new(subscriber),
                });
                Box::into_raw(sub_handle) as *mut c_void
            }
            Err(e) => {
                set_error(format!("Failed to declare liveliness subscriber: {}", e));
                ptr::null_mut()
            }
        }
    });
    
    match result {
        Ok(ptr) => ptr,
        Err(_) => {
            set_error("Panic occurred in zenoh_liveliness_declare_subscriber");
            ptr::null_mut()
        }
    }
}

// ============== Session Info ==============

/// Gets the Zenoh ID of the session as a hex string.
/// Returns a C string that must be freed with zenoh_free_string.
/// The format is a stable hex representation of the ZenohId bytes.
#[no_mangle]
pub extern "C" fn zenoh_session_zid(session: *const c_void) -> *mut c_char {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if session.is_null() {
            set_error("Session pointer is null");
            return ptr::null_mut();
        }

        let handle = unsafe { &*(session as *const SessionWrapper) };
        let zid = handle.session.zid();
        // Use stable hex representation instead of Debug format
        let zid_bytes = zid.to_le_bytes();
        let zid_str: String = zid_bytes.iter().map(|b| format!("{:02x}", b)).collect();

        match CString::new(zid_str) {
            Ok(cstr) => cstr.into_raw(),
            Err(e) => {
                set_error(format!("Failed to create ZID string: {}", e));
                ptr::null_mut()
            }
        }
    });
    
    match result {
        Ok(ptr) => ptr,
        Err(_) => {
            set_error("Panic occurred in zenoh_session_zid");
            ptr::null_mut()
        }
    }
}

// ============== Encoding Helpers ==============

fn encoding_to_id(encoding: &zenoh::bytes::Encoding) -> ZenohEncodingId {
    let enc_str = encoding.to_string();
    match enc_str.as_str() {
        "application/octet-stream" => ZenohEncodingId::AppOctetStream,
        "text/plain" => ZenohEncodingId::TextPlain,
        "application/json" => ZenohEncodingId::AppJson,
        "text/json" => ZenohEncodingId::TextJson,
        "application/cbor" => ZenohEncodingId::AppCbor,
        "application/yaml" => ZenohEncodingId::AppYaml,
        "text/yaml" => ZenohEncodingId::TextYaml,
        "text/xml" => ZenohEncodingId::TextXml,
        "application/xml" => ZenohEncodingId::AppXml,
        "text/csv" => ZenohEncodingId::TextCsv,
        "application/protobuf" => ZenohEncodingId::AppProtobuf,
        "text/html" => ZenohEncodingId::TextHtml,
        "" => ZenohEncodingId::Empty,
        _ => ZenohEncodingId::AppOctetStream,
    }
}

fn id_to_encoding(id: ZenohEncodingId) -> zenoh::bytes::Encoding {
    use zenoh::bytes::Encoding;
    match id {
        ZenohEncodingId::Empty => Encoding::default(),
        ZenohEncodingId::AppOctetStream => Encoding::APPLICATION_OCTET_STREAM,
        ZenohEncodingId::TextPlain => Encoding::TEXT_PLAIN,
        ZenohEncodingId::AppJson => Encoding::APPLICATION_JSON,
        ZenohEncodingId::TextJson => Encoding::TEXT_JSON,
        ZenohEncodingId::AppCbor => Encoding::APPLICATION_CBOR,
        ZenohEncodingId::AppYaml => Encoding::APPLICATION_YAML,
        ZenohEncodingId::TextYaml => Encoding::TEXT_YAML,
        ZenohEncodingId::TextXml => Encoding::TEXT_XML,
        ZenohEncodingId::AppXml => Encoding::APPLICATION_XML,
        ZenohEncodingId::TextCsv => Encoding::TEXT_CSV,
        ZenohEncodingId::AppProtobuf => Encoding::APPLICATION_PROTOBUF,
        ZenohEncodingId::TextHtml => Encoding::TEXT_HTML,
    }
}

// ============== Put with Encoding ==============

/// Publishes data with encoding on the given publisher.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_publisher_put_with_encoding(
    publisher: *mut c_void,
    payload: *const u8,
    payload_len: usize,
    encoding_id: ZenohEncodingId,
) -> ZenohError {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if publisher.is_null() {
            set_error("Publisher pointer is null");
            return ZenohError::NullPointer;
        }

        if payload.is_null() && payload_len > 0 {
            set_error("Payload pointer is null but length > 0");
            return ZenohError::NullPointer;
        }

        let handle = unsafe { &*(publisher as *const PublisherWrapper) };
        let data = if payload.is_null() || payload_len == 0 {
            Vec::new()
        } else {
            unsafe { std::slice::from_raw_parts(payload, payload_len) }.to_vec()
        };

        let encoding = id_to_encoding(encoding_id);

        let put_result = run_blocking(async move {
            handle.publisher.put(data).encoding(encoding).await
        });

        match put_result {
            Ok(_) => ZenohError::Ok,
            Err(e) => {
                set_error(format!("Put with encoding failed: {}", e));
                ZenohError::PutFailed
            }
        }
    });
    
    match result {
        Ok(err) => err,
        Err(_) => {
            set_error("Panic occurred in zenoh_publisher_put_with_encoding");
            ZenohError::Panic
        }
    }
}

/// Puts data directly on a key expression with encoding.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_put_with_encoding(
    session: *mut c_void,
    key_expr: *const c_char,
    payload: *const u8,
    payload_len: usize,
    encoding_id: ZenohEncodingId,
) -> ZenohError {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if session.is_null() {
            set_error("Session pointer is null");
            return ZenohError::NullPointer;
        }
        if key_expr.is_null() {
            set_error("Key expression is null");
            return ZenohError::NullPointer;
        }

        if payload.is_null() && payload_len > 0 {
            set_error("Payload pointer is null but length > 0");
            return ZenohError::NullPointer;
        }

        let handle = unsafe { &*(session as *const SessionWrapper) };
        let key = unsafe {
            match CStr::from_ptr(key_expr).to_str() {
                Ok(s) => s,
                Err(e) => {
                    set_error(format!("Invalid UTF-8 in key expression: {}", e));
                    return ZenohError::InvalidKeyExpr;
                }
            }
        };

        let data = if payload.is_null() || payload_len == 0 {
            Vec::new()
        } else {
            unsafe { std::slice::from_raw_parts(payload, payload_len) }.to_vec()
        };

        let encoding = id_to_encoding(encoding_id);

        let put_result = run_blocking(async move {
            handle.session.put(key, data).encoding(encoding).await
        });

        match put_result {
            Ok(_) => ZenohError::Ok,
            Err(e) => {
                set_error(format!("Put with encoding failed: {}", e));
                ZenohError::PutFailed
            }
        }
    });
    
    match result {
        Ok(err) => err,
        Err(_) => {
            set_error("Panic occurred in zenoh_put_with_encoding");
            ZenohError::Panic
        }
    }
}

// ============== Put with Attachment ==============

/// Puts data with attachment on a key expression.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_put_with_attachment(
    session: *mut c_void,
    key_expr: *const c_char,
    payload: *const u8,
    payload_len: usize,
    attachment_items: *const ZenohAttachmentItem,
    attachment_count: usize,
) -> ZenohError {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if session.is_null() {
            set_error("Session pointer is null");
            return ZenohError::NullPointer;
        }
        if key_expr.is_null() {
            set_error("Key expression is null");
            return ZenohError::NullPointer;
        }

        if payload.is_null() && payload_len > 0 {
            set_error("Payload pointer is null but length > 0");
            return ZenohError::NullPointer;
        }

        let handle = unsafe { &*(session as *const SessionWrapper) };
        let key = unsafe {
            match CStr::from_ptr(key_expr).to_str() {
                Ok(s) => s,
                Err(e) => {
                    set_error(format!("Invalid UTF-8 in key expression: {}", e));
                    return ZenohError::InvalidKeyExpr;
                }
            }
        };

        let data = if payload.is_null() || payload_len == 0 {
            Vec::new()
        } else {
            unsafe { std::slice::from_raw_parts(payload, payload_len) }.to_vec()
        };

        // Build attachment as serialized bytes
        let attachment_bytes: Option<Vec<u8>> = if !attachment_items.is_null() && attachment_count > 0 {
            let items = unsafe { std::slice::from_raw_parts(attachment_items, attachment_count) };
            let mut serialized = Vec::new();
            for item in items {
                if item.key.is_null() {
                    continue;
                }
                let key_bytes = unsafe {
                    match CStr::from_ptr(item.key).to_str() {
                        Ok(s) => s.as_bytes(),
                        Err(_) => continue,
                    }
                };
                let value = if item.value.is_null() || item.value_len == 0 {
                    &[]
                } else {
                    unsafe { std::slice::from_raw_parts(item.value, item.value_len) }
                };
                // Simple format: key_len(4) + key + value_len(4) + value
                serialized.extend_from_slice(&(key_bytes.len() as u32).to_le_bytes());
                serialized.extend_from_slice(key_bytes);
                serialized.extend_from_slice(&(value.len() as u32).to_le_bytes());
                serialized.extend_from_slice(value);
            }
            if serialized.is_empty() { None } else { Some(serialized) }
        } else {
            None
        };

        let put_result = run_blocking(async move {
            if let Some(att_bytes) = attachment_bytes {
                handle.session.put(key, data).attachment(att_bytes).await
            } else {
                handle.session.put(key, data).await
            }
        });

        match put_result {
            Ok(_) => ZenohError::Ok,
            Err(e) => {
                set_error(format!("Put with attachment failed: {}", e));
                ZenohError::PutFailed
            }
        }
    });
    
    match result {
        Ok(err) => err,
        Err(_) => {
            set_error("Panic occurred in zenoh_put_with_attachment");
            ZenohError::Panic
        }
    }
}

// ============== Querier ==============

/// Declares a querier for repeated queries on the same key expression.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_declare_querier(
    session: *mut c_void,
    key_expr: *const c_char,
) -> *mut c_void {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if session.is_null() {
            set_error("Session pointer is null");
            return ptr::null_mut();
        }
        if key_expr.is_null() {
            set_error("Key expression is null");
            return ptr::null_mut();
        }

        let handle = unsafe { &*(session as *const SessionWrapper) };
        let key = unsafe {
            match CStr::from_ptr(key_expr).to_str() {
                Ok(s) => s,
                Err(e) => {
                    set_error(format!("Invalid UTF-8 in key expression: {}", e));
                    return ptr::null_mut();
                }
            }
        };

        let session_arc = handle.session.clone();
        let querier_result = run_blocking(async move {
            handle.session.declare_querier(key).await
        });

        match querier_result {
            Ok(querier) => {
                let static_querier: zenoh::query::Querier<'static> = unsafe {
                    std::mem::transmute(querier)
                };
                let q_handle = Box::new(QuerierWrapper {
                    querier: static_querier,
                    _session: session_arc,
                });
                Box::into_raw(q_handle) as *mut c_void
            }
            Err(e) => {
                set_error(format!("Failed to declare querier: {}", e));
                ptr::null_mut()
            }
        }
    });
    
    match result {
        Ok(ptr) => ptr,
        Err(_) => {
            set_error("Panic occurred in zenoh_declare_querier");
            ptr::null_mut()
        }
    }
}

/// Performs a get query using the querier.
/// The callback receives SampleData pointers that are valid only during the callback invocation.
/// Do not store these pointers for later use.
/// Call zenoh_last_error() for error details.
#[no_mangle]
pub extern "C" fn zenoh_querier_get(
    querier: *mut c_void,
    callback: ZenohGetCallback,
    context: *mut c_void,
) -> ZenohError {
    clear_error();
    
    let result = panic::catch_unwind(|| {
        if querier.is_null() {
            set_error("Querier pointer is null");
            return ZenohError::NullPointer;
        }

        let handle = unsafe { &*(querier as *const QuerierWrapper) };
        let context_ptr = context as usize;

        let get_result = run_blocking_local(async {
            handle.querier
                .get()
                .callback(move |reply| {
                    if let Ok(sample) = reply.result() {
                        let key_cstr = match CString::new(sample.key_expr().as_str()) {
                            Ok(s) => s,
                            Err(_) => return,
                        };

                        let payload = sample.payload().to_bytes();
                        let kind = match sample.kind() {
                            SampleKind::Put => ZenohSampleKind::Put,
                            SampleKind::Delete => ZenohSampleKind::Delete,
                        };

                        let encoding_id = encoding_to_id(sample.encoding());
                        let (timestamp_valid, timestamp) = match sample.timestamp() {
                            Some(ts) => {
                                let ntp = ts.get_time().as_u64();
                                let id_bytes = ts.get_id().to_le_bytes();
                                let mut id = [0u8; 16];
                                id.copy_from_slice(&id_bytes[..16.min(id_bytes.len())]);
                                (true, ZenohTimestamp { time_ntp64: ntp, id })
                            }
                            None => (false, ZenohTimestamp { time_ntp64: 0, id: [0u8; 16] }),
                        };

                        let c_sample = SampleData {
                            key_expr: key_cstr.as_ptr(),
                            payload_data: payload.as_ptr(),
                            payload_len: payload.len(),
                            kind,
                            encoding_id,
                            timestamp_valid,
                            timestamp,
                        };

                        unsafe {
                            callback(&c_sample, context_ptr as *mut c_void);
                        }
                    }
                })
                .await
        });

        match get_result {
            Ok(_) => ZenohError::Ok,
            Err(e) => {
                set_error(format!("Querier get failed: {}", e));
                ZenohError::Unknown
            }
        }
    });
    
    match result {
        Ok(err) => err,
        Err(_) => {
            set_error("Panic occurred in zenoh_querier_get");
            ZenohError::Panic
        }
    }
}

/// Undeclares and frees a querier.
#[no_mangle]
pub extern "C" fn zenoh_undeclare_querier(querier: *mut c_void) {
    if querier.is_null() {
        return;
    }
    let _ = panic::catch_unwind(|| {
        unsafe {
            let _ = Box::from_raw(querier as *mut QuerierWrapper);
        }
    });
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

        extern "C" fn test_callback(_sample: *const SampleData, _context: *mut c_void) {}

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
