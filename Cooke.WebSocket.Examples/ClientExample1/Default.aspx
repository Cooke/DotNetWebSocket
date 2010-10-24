<%@ Page Title="Home Page" Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs"
    Inherits="ClientExample1._Default" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" 
    "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
	<meta http-equiv="Content-Type" content="text/html; charset=UTF-8" /> 
	<script type="text/javascript" src="/js/mootools-core.js"></script>
	<script type="text/javascript" src="/js/mootools-more.js"></script>  
    <script type="text/javascript">
    window.addEvent('domready', function() {
	    var chatscroll = new Object();
	
	    chatscroll.Pane = function(scrollContainerId){
	      this.bottomThreshold = 20;
	      this.scrollContainerId = scrollContainerId;
	      this._lastScrollPosition = 100000000;
	    }
	
	    chatscroll.Pane.prototype.activeScroll = function(){
	
	      var _ref = this;
	      var scrollDiv = document.getElementById(this.scrollContainerId);
	      var currentHeight = 0;
	      
	      var _getElementHeight = function(){
	        var intHt = 0;
	        if(scrollDiv.style.pixelHeight)intHt = scrollDiv.style.pixelHeight;
	        else intHt = scrollDiv.offsetHeight;
	        return parseInt(intHt);
	      }
	
	      var _hasUserScrolled = function(){
	        if(_ref._lastScrollPosition == scrollDiv.scrollTop || _ref._lastScrollPosition == null){
	          return false;
	        }
	        return true;
	      }
	
	      var _scrollIfInZone = function(){
	        if( !_hasUserScrolled || 
	            (currentHeight - scrollDiv.scrollTop - _getElementHeight() <= _ref.bottomThreshold)){
	            scrollDiv.scrollTop = currentHeight;
	            _ref._isUserActive = false;
	        }
	      }
	
	
	      if (scrollDiv.scrollHeight > 0)currentHeight = scrollDiv.scrollHeight;
	      else if(scrollDiv.offsetHeight > 0)currentHeight = scrollDiv.offsetHeight;
	
	      _scrollIfInZone();
	
	      _ref = null;
	      scrollDiv = null;
	
	    }
	
	    	var socket;
	    	
	    	try 
	    	{
	    		socket = new WebSocket('ws://localhost:8181/testresource');
	    	} 
	    	catch (err) 
	    	{
	    	 print('Error: ' + err);
	    	}
	    	        
	        socket.onopen = function() {
	            print('handshake successfully established. May send data now...');
	        };
			socket.onmessage = function(msg) {
				print(msg.data);
			}
	        socket.onclose = function(msg) {
	            print(msg.type);
	            print('connection closed');
	        };
			socket.onerror = function(msg)
			{
				print('Error: ' + msg.type);
			}
	
			function print(msg)
			{
				var out = $('out');
				out.appendText(msg).grab(new Element('br'));
				divScroll.activeScroll();	
			}
	
			var divScroll = new chatscroll.Pane('out');	
	});	
    </script>		
</head>
<body style="margin: 0;">

<div id="out" style="background-color: black; position: fixed; bottom: 30px; top: 0; left: 0; right: 0; color: lightgreen; padding: 10px; font-family: arial; overflow: auto;">
</div>

<div style="position: fixed; bottom: 0; left: 0; right: 0; height: 30px; background-color: black; border-top: solid 1px white;">
<form type="submit" id="form">
<input type="text" id="cl" style="width: 94%; margin-left: 5px; margin-top: 5px;" />
<input type="button" id="send" value="Send" />
</form>
</div>

<script type="text/javascript">
	$('form').addEvent('submit', function(e) {
		e.stop();		
		socket.send($('cl').get('value'));
		$('cl').set('value', '');		
	});
</script>
	
</body>
</html>
