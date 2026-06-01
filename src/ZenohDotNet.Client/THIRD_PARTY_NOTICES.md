# Third-Party Notices

This project bundles or redistributes third-party software.

## Eclipse Zenoh (Rust)

The native shared libraries (`zenoh_ffi.dll`, `libzenoh_ffi.dylib`, `libzenoh_ffi.so`, and static archives for mobile targets) shipped in this repository and its NuGet/UPM packages statically link against the Eclipse Zenoh Rust crate.

- Project: https://projects.eclipse.org/projects/iot.zenoh
- Source: https://github.com/eclipse-zenoh/zenoh (release `1.9.0`)
- License: **Apache License 2.0** (chosen from the Eclipse Public License 2.0 OR Apache License 2.0 dual license)
- SPDX-License-Identifier: Apache-2.0
- Full license text: [licenses/zenoh-APACHE-2.0.txt](licenses/zenoh-APACHE-2.0.txt)
- NOTICE: [licenses/zenoh-NOTICE.md](licenses/zenoh-NOTICE.md)

The `zenoh_ffi` Rust crate in this repository (MIT) is a thin FFI wrapper around the Zenoh library. Only the embedded Zenoh runtime inside the native binaries is subject to Apache-2.0 redistribution terms.

### Trademark notice

Eclipse Zenoh is a trademark of the Eclipse Foundation. Eclipse, and the Eclipse Logo are registered trademarks of the Eclipse Foundation.
