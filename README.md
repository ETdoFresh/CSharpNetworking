# C# Networking
This project is the basic implementation of these low level networking protocols

- **TCP** (Server and Client)
- **WebSocket** (~~Server and~~ Client)
- ~~**UDP** (Server and Client)~~

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
The Servers in this project use the SocketStream class \<T> to represent a client connection. It includes the client's Socket and NetworkStream.

### Client Events
- `Opened()` When the client connects to the server
- `Received(byte[] bytes)` When the client receives a bytes from the server
- `Sent(byte[] bytes)` When the client sends a bytes to the server
- `Closed()` When the client disconnects from the server
- `Error(Exception exception)` When there is an exception on the client
