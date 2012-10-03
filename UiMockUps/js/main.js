// This set's up the module paths for underscore and backbone
require.config({
    //To get timely, correct error triggers in IE, force a define/shim exports check.
    //enforceDefine: true,
    'paths': {
        "noext": 'libs/noext',
        "underscore": "libs/underscore-min",
        "backbone": "libs/backbone-min",
        "bootstrap":"libs/bootstrap.min",
        "scrollTo":"libs/jquery.scrollTo-min",
        "throttle":"libs/jquery.ba-throttle-debounce.min",
        "signalr": "libs/jquery.signalR-0.5.3",
        "json2": "libs/json2.min"
    },
    'shim':
    {
        "throttle":{
            deps: ["jquery"]
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



