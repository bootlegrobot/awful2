

function loadHTML(html) {
    window.external.notify('htmlloading');
    document.body.innerHTML = html;
    window.external.notify('htmlloaded');
}

function clearHtml() {
    document.body.innerHTML = '';
}

function showImageMenu(link, demarc) {
    window.external.notify('image' + demarc + link);
}

function getVerticalScrollPosition() {
    return document.body.scrollTop.toString();
}

function setVerticalScrollPosition(position) {
    document.body.scrollTop = position;
}

function getHorizontalScrollPosition() {
    return document.body.scrollLeft.toString();
}

function setHorizontalScrollPosition(position) {
    document.body.scrollLeft = position;
}

function reportVScrollPosition() {
    var position = getVerticalScrollPosition();
    window.external.notify('vscroll' + '##$$##' + position);
}

function openPostMenu(id) {
    window.external.notify('post' + '##$$##' + id);
}

function viewport() {

    var viewportwidth;
    var viewportheight;

    // the more standards compliant browsers (mozilla/netscape/opera/IE7) use window.innerWidth and window.innerHeight

    if (typeof window.innerWidth != 'undefined') {
        viewportwidth = window.innerWidth,
      viewportheight = window.innerHeight
    }

    // IE6 in standards compliant mode (i.e. with a valid doctype as the first line in the document)

    else if (typeof document.documentElement != 'undefined'
     && typeof document.documentElement.clientWidth !=
     'undefined' && document.documentElement.clientWidth != 0) {
        viewportwidth = document.documentElement.clientWidth,
       viewportheight = document.documentElement.clientHeight
    }

    // older versions of IE

    else {
        viewportwidth = document.getElementsByTagName('body')[0].clientWidth,
       viewportheight = document.getElementsByTagName('body')[0].clientHeight
    }
    window.external.notify('Your viewport width is ' + viewportwidth + 'x' + viewportheight);
}

function set_styles(foreground, background, accent, seen, chrome, fontsize) {
    try {

        // set the foreground color of the view
        document.fgColor = foreground;

        // set the document background color
        $("html").css("background", background);

        // set the background color
        $("body").css("background", background);

        // set the font-size
        $("body").css("font-size", fontsize + 'px');

        // set the seen color
        $(".post_content_seen").css("color", seen);

        // set the accents
        $(".quote-author").css("color", accent);
        $("blockquote").css("border-left", "2px solid " + accent);

        // invoke callback
        window.external.notify("styleset");
    }

    catch (err) {
        window.external.notify('error' + demarc + err.description);
    }
}

function set_fontsize(fontsize) {
    // set the font-size
    $("body").css("font-size", fontsize + 'px');
}

function show_image(trigger, id, link, demarc) {
    try {
        var t = document.getElementById(trigger);
        var e = document.getElementById(id);
        
        // viewport();
       
        //e.style.minHeight = '10%';
        //e.style.maxWidth = '250px';
        e.src = link;
        t.style.display = 'none';
        e.style.display = 'block';
    }
    catch (err) {
        window.external.notify('error' + demarc + err.description);
    }
}

function show_quoted_image(trigger, id, link, demarc) {
    try {
        var t = document.getElementById(trigger);
        var e = document.getElementById(id);

        // viewport();

        e.style.minHeight = '10%';
        e.style.maxWidth = '200px';
        e.src = link;
        t.style.display = 'none';
        e.style.display = 'block';
    }
    catch (err) {
        window.external.notify('error' + demarc + err.description);
    }
}

function show_spoiler(trigger, id, demarc) {
    var t = document.getElementById(trigger);
    var e = document.getElementById(id);
    var s = getVerticalScrollPosition();

    e.style.display = 'inline';
    t.style.display = 'none';
    window.external.notify('scroll' + demarc + s);
}

function navigate(url) {
    window.external.notify(url);
}

function scrollTo(id, isSmooth) {
    try {
        if (isSmooth == 'true') {
            var link = document.getElementById(id);
            //window.external.notify('smooth: true, ' + link.href);
            clickLink(link); 
        }
        else {
            var divid = document.getElementById(id);
            //window.external.notify('smooth: false, ' + divid.id);
            divid.scrollIntoView(true);
        }
    }
    catch (err) {
        window.external.notify('error' + '##$$##' + err.description);
    }
}

function clickLink(link) {

    var cancelled = false;

    if (document.createEvent) {
        var event = document.createEvent("MouseEvents");
        event.initMouseEvent("click", true, true, window,
                0, 0, 0, 0, 0,
                false, false, false, false,
                0, null);
        cancelled = !link.dispatchEvent(event);
    }
    else if (link.fireEvent) {
        cancelled = !link.fireEvent("onclick");
    }

    if (!cancelled) {
        window.location = link.href;
    }
}

$(document).ready(function () {
   

});
