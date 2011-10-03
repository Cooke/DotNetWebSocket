<%@ Page Title="Home Page" Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ClientExample1._Default" %>

<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=UTF-8" />
    <script type="text/javascript">
        function print(msg) {
            document.getElementById('out').appendChild(document.createTextNode(msg));
            document.getElementById('out').appendChild(document.createElement('br'));
        };

        window.onload = function () {
            print('Hej!');

            var socket;

            try {
                socket = new WebSocket('ws://localhost:8181/testresource');
            }
            catch (err) {
                print('Error: ' + err);
            }

            socket.onopen = function () {
                print('Connection established');
            };

            socket.onmessage = function (msg) {
                print('Message received: ' + msg.data);
            };

            socket.onclose = function (msg) {
                print('Connection closed: ' + msg.type);
            };

            socket.onerror = function (msg) {
                print('Error: ' + msg.type);
            };

            document.getElementById('send').onclick = function (e) {
                e.stopPropagation();
                e.cancelBubble = true;

                var text = document.getElementById('text');
                print('Send: ' + text.value);
                
                socket.send(text.value);
                text.value = '';

                return false;
            };
        };
    </script>
</head>
<body style="margin: 0;">
    <div id="out" style="background-color: black; position: fixed; bottom: 30px; top: 0; left: 0; right: 0; color: lightgreen; padding: 10px; font-family: arial; overflow: auto;">
    </div>
    <div style="position: fixed; bottom: 0; left: 0; right: 0; height: 30px; background-color: black; border-top: solid 1px white;">
        <input type="text" id="text" style="width: 94%; margin-left: 5px; margin-top: 5px;" />
        <input type="button" id="send" value="Send" />
    </div>
</body>
</html>
