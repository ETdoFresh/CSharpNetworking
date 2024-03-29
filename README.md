# C# Networking
This project is a basic implementation of these low level networking protocols

- **TCP** (Server and Client)
- **WebSocket** (Server and Client)
- **UDP** (Server and Client)

## Abstract Classes
Each protocol implements abstract class Server or Client which require that the protocol launches the following events when:

### Server\<T> Events
- `ServerOpened()` When the server is started and listening for clients
- `ServerClosed()` When the server stops listening for clients
- `ServerError(Exception exception)` When there is an exception on the server
- `ClientConnected(T client)` When a client connects to the server
- `ClientDisconnected(T client)` When a client disconnects from the server
- `ClientReceived(T client, byte[] bytes)` When the server receives a bytes from a client
- `ClientSent(T client, byte[] bytes)` When the server sends a bytes to a client
- `ClientError(T client, Exception exception)` When there is an exception with the client connection

### SocketStream Class
The Servers in this project use `SocketStream` class as _T_ to represent a client connection. It includes the client's _Socket_ and _NetworkStream_.

### Client Events
- `Opened()` When the client connects to the server
- `Received(byte[] bytes)` When the client receives a bytes from the server
- `Sent(byte[] bytes)` When the client sends a bytes to the server
- `Closed()` When the client disconnects from the server
- `Error(Exception exception)` When there is an exception on the client

### WebSocket Server Certificates
A `wss://` connection requires a certificate. In order to generate a self-signed [localhost] `certificate.pfx` file, you can follow these steps on Windows:

1. Install/Ensure you have `OpenSSL` installed. If you have chocolatey...
```bash
choco install openssl
```
2. Generate private key:  
```bash
openssl genrsa -out certificate.key 2048
```
3. Generate certificate signing request:  
```bash
openssl req -new -key certificate.key -out certificate.csr
```
4. Generate self-signed certificate:  
```bash
openssl x509 -req -days 365 -in certificate.csr -signkey certificate.key -out certificate.crt
```
5. Generate PFX file:  
```bash
openssl pkcs12 -export -out certificate.pfx -inkey certificate.key -in certificate.crt
```
6. Place the `certificate.pfx` file in root of repository directory
7. Additionally, you have to let Windows know this is a trusted certificate.
   1. Double click on the `certificate.pfx` file
   2. Store Location: `Local Machine`
   3. `Place all certificates in the following store`: `Trusted Root Certification Authorities`

### Caveats
As shown in the examples, this implementation uses buffers and streams. If the data is larger than the buffer, it will be split into multiple packets. This is not handled in the example, but it is something to be aware of when implementing this in your production environment.

Udp throws an exception when trying to send packets bigger than the buffer size or when sending to a non-existent endpoint. This is not handled in the example, but it is something to be aware of when implementing this in your production environment.