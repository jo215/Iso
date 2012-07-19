//	Disables user selection of elements
function disableSelection(target)
{
    if (typeof target.onselectstart != "undefined") //IE route
        target.onselectstart = function () { return false }
    else if (typeof target.style.MozUserSelect != "undefined") //Firefox route
        target.style.MozUserSelect = "none"
    else //All other route (ie: Opera)
        target.onmousedown = function () { return false }
    target.style.cursor = "default"
}

//	Clears all messages displayed in the message area
function clearElement(element)
{
	var cell = document.getElementById(element);
	if ( cell.hasChildNodes() )
	    while ( cell.childNodes.length >= 1 )
	        cell.removeChild( cell.firstChild );
}

//	Displays a new message in the message area
function addMessage(message, color)
{
	var element = document.createElement('h5');
	element.style.color = color;
	element.textContent = message;
	document.getElementById('messages').appendChild(element);
}

//	Adds a new player to the list
function addPlayer(sessionid, name, color)
{
	var element = document.createElement('h5');
	element.style.color = color;
	element.textContent = sessionid + " " + name;
	document.getElementById('players').appendChild(element);
}

//	Adds a new map to the list
function addMap(name)
{
	var element = document.createElement('li');
	element.innerHTML = "<h6 onclick='UIEventManager.click(\"selectmap\", \"" + name + "\");' id='" + name + "'>" + name + "</h6>";
	document.getElementById('maplist').appendChild(element);
}