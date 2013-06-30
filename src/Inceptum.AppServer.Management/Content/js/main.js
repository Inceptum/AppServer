// This set's up the module paths for underscore and backbone
require.config({
    //To get timely, correct error triggers in IE, force a define/shim exports check.
    //enforceDefine: true,
    'paths': {
        "noext": 'libs/noext',
        "underscore": "libs/underscore-min",
//        "backbone": "libs/backbone-min",
        "backbone": "libs/backbone",
        "bootstrap":"libs/bootstrap.min",
        "scrollTo":"libs/jquery.scrollTo-min",
        "shortcut":"libs/shortcut",
        "throttle":"libs/jquery.ba-throttle-debounce.min",
        "signalr": "libs/jquery.signalR-1.1.2",
        "json2": "libs/json2.min",
        "codemirror": "libs/codemirror/codemirror",
        "codemirrorjs": "libs/codemirror/mode/javascript",
        "fileupload": "libs/jquery.fileupload",
        "jquery.ui.widget": "libs/jquery.ui.widget",
        "bootbox": "libs/bootbox"
    },
    'shim':
    {
        "fileupload":{
            deps: ["jquery","libs/jquery.iframe-transport","jquery.ui.widget"]
        },
        "bootbox":{
            deps: ["bootstrap"]
        },
        "throttle":{
            deps: ["jquery"]
        },
        "codemirrorjs":{
            deps: ["codemirror"]
        },
        "signalr": {
            deps: ["json2"],
            deps: ["jquery"]

        },
        "noext!sr/signalr/hubs":{
            deps: ["signalr"]
        },
        backbone: {
            'deps': ['jquery', 'underscore'],
            'exports': 'Backbone'
        },
        underscore: {
            'exports': '_'
        },
        'scrollTo': {
			 deps:		['jquery']
		},
        'hotkeys': {
			 deps:		['jquery']
		}
    }
});


require([
    'underscore',
    'backbone',
    'app',
    'signalr',
    "bootstrap"
],function(_, Backbone, app){
    jQuery.support.cors = true;
    app.init();
});



