// Write your JavaScript code.

var loaded = [];

$(".accordian-toggle").on("click", function (e) {
    var current = e.currentTarget;
    
    var id = current.getAttribute("data-target");

    if (loaded.indexOf(id) !== -1)
        return;
    else
        loaded.push(id);

    var temp = current.nextElementSibling;

    if (temp) {
        var mapFrame = temp.firstElementChild.firstElementChild.firstElementChild;
        var key = current.querySelector(".ticketkey").firstElementChild.getAttribute("value");
        var src = "http://www.managetickets.com/morecApp/popupGoogleMap.jsp?key=" + key + "&db=ia&sas=N";

        mapFrame.setAttribute("src", src);
    } else {
        // IE7
        var mapFrame = current.nextSibling.firstChild.firstChild.firstChild;
        var key = current.children[1].firstChild.attributes['value'].nodeValue;
        var src = "http://www.managetickets.com/morecApp/popupGoogleMap.jsp?key=" + key + "&db=ia&sas=N";
    
        mapFrame.attributes['src'].nodeValue = src;
    }
});

$(".hiddenRow").on("click", function (e) {
    var prev = e.currentTarget.parentElement.previousElementSibling;
    prev.click();
});

$("#sortTicketNumber").on("click", function () {
    console.log('elel');
    reload("TicketNumber", true);
});

$("#sortTicketType").on("click", function () {
    reload("TicketType", false);
});

$("#sortStatus").on("click", function () {
    reload("Status", false);
});

$("#sortOriginalCallDate").on("click", function () {
    reload("OriginalCallDate", true);
});

$("#sortBeginWorkDate").on("click", function () {
    reload("BeginWorkDate", true);
});

$("#sortStreetAddress").on("click", function () {
    reload("StreetAddress", false);
});

$("#sortCity").on("click", function () {
    reload("City", false);
});

$("#sortExcavatorName").on("click", function () {
    reload("ExcavatorName", false);
});

$("#sortOnsightContactPerson").on("click", function () {
    reload("OnsightContactPerson", false);
});

$("#sortOnsightContactPhone").on("click", function () {
    reload("OnsightContactPhone", false);
});

$("#sortWorkExtent").on("click", function () {
    reload("WorkExtent", false);
});

$("#sortRemarks").on("click", function () {
    reload("Remarks", false);
});

function reload(column, defaultOrder) {
    var currentColumn = sessionStorage.getItem("column");
    var currentOrder = sessionStorage.getItem("desc");
    var order;

    if (currentColumn === column) {
        if (currentOrder === null)
            order = defaultOrder;
        else if (currentOrder === "true")
            order = false;
        else
            order = true;
        
        sessionStorage.setItem("desc", order);
    } else {
        order = defaultOrder;
        sessionStorage.setItem("column", column);
        sessionStorage.setItem("desc", order);
    }

    window.location = "/?order=" + column + "&desc=" + order;
}


// polyfills for IE 8
if (!Array.prototype.indexOf) {
    Array.prototype.indexOf = function (obj, start) {
        for (var i = (start || 0), j = this.length; i < j; i++) {
            if (this[i] === obj) { return i; }
        }
        return -1;
    }
}

if (!("nextElementSibling" in document.documentElement)) {
    Object.defineProperty(Element.prototype, "nextElementSibling", {
        get: function () {
            var e = this.nextSibling;
            while (e && 1 !== e.nodeType)
                e = e.nextSibling;
            return e;
        }
    });
}

if (!("firstElementChild" in document.documentElement)) {
    Object.defineProperty(Element.prototype, "firstElementChild", {
        get: function () {
            var node, nodes = this.childNodes, i = 0;
            while (node = nodes[i++]) {
                if (node.nodeType === 1) {
                    return node;
                }
            }
            return null;
        }
    });
}
 