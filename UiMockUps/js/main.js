// This set's up the module paths for underscore and backbone
require.config({
    'paths': {
        "noext": 'lib/noext',
        "underscore": "libs/underscore-min",
        "backbone": "libs/backbone-min",
        "bootstrap":"libs/bootstrap.min",
        "scrollTo":"libs/jquery.scrollTo-min",
        "signalr": "libs/jquery.signalR-0.5.3.min",
        "signalrHubs": '/sr/signalr/hubs?'
    },
    'shim':
    {
        "signalr": {
            deps: ["jquery"]
        },
        "signalrHubs":{
            deps: ["signalr"]
        },
        backbone: {
            'deps': ['jquery', 'underscore'],
            'exports': 'Backbone'
        },
        underscore: {
            'exports': '_'
        },
        'scrollTo': ['jquery']
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



