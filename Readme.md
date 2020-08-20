# EZChat-Unity

This asset allows you to simply create a TCP-chat in the unity engine.

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See deployment for notes on how to deploy the project on a live system.

### Prerequisites

What things you need to use the software

```
Unity-Engine
```

### Installing

This is how you can set up the Project. You can skip step 2 and step 3, if you want to host the server on localhost and stick with the standard settings.

1. Clone/download the project. Make sure you have got the folders "EZ Chat" and "EZ Chat Server"

2. Go to folder "EZ Chat Server\EZ Chat Server\bin\Release" and open the "config.json" with any text editor.
	It should look like this:

```
{
"host": "127.0.0.1",
"port": 6000,
"name": "EZChat",
"sendConnectMessage": true
}
```

3. Edit the config.json. You must provide valid options for:
	- Host 
	- Port
	- Name
	- Send a connect Message, if a new client connects?
	
4. Import your "EZ Chat" project into Unity and open it.

5. Open the "EZChatExampleScene" in your "Scenes"-folder.

6. Select the GameObject "EZChatClient" and edit the values on the "EZChatClient"-Component. Make sure you enter a name, the correct host and port. In the example above host is "localhost" and port is "6000".

7. Start your server from Visual-Studio or execute the "EZ Chat Server.exe" in folder "EZ Chat Server\EZ Chat Server\bin\Release"

8. Press play in Unity. This message should appear in chat:

```
<name> has joined the chat.
```

This is how it works. If you have any problems, contact rickjunge0@gmail.com

## Authors

* **Rick Junge** - *Initial work* - [rickjunge](https://github.com/rickjunge)

## License

No license.
