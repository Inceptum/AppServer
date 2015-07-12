define([
    'jquery',
    'backbone',
    'underscore'],
    function($, Backbone, _){
        return  {
            baseUrl:"http://app.inceptumsoft.com",
            httpPort:9223,
            signalR:{
                port:"9223",
                crossDomain:true
            },
            httpUrl:function(path){return path;},
            signalRUrl:function(path){return path;}
        }
    });