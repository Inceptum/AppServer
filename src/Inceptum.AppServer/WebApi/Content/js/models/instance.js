define(['jquery', 'backbone', 'underscore','context'], function($, Backbone, _,context){

    var Model = Backbone.Model.extend({
        urlRoot: context.httpUrl("/api/instances"),
        initialize: function() {
            _.bindAll(this, "command","start","debug","stop","kill");
        },
        idAttribute: "id",
        start:function(options){
            this.action("start",options);
        },
        debug: function (options) {
            this.action("debug", options);
        },
        restart:function(options){
            this.action("restart",options);
        },
        stop:function(options){
            this.action("stop",options);
        },
        kill:function(options){
            this.action("kill", options);
        },
        action:function(action,options){
            var id = this.id;
            var self=this;
            $.ajax({
                url: context.httpUrl('/api/instances/'+id+'/'+action),
                type: 'POST',
                success: function () {
                    if(options.success)
                        options.success(self);
                },
                error: function (args) {
                    if(options.error)
                        options.error(self,args);
                }
            });
        },
        command:function(command,options){
            var id = this.id;
            var self=this;
            $.ajax({
                url: context.httpUrl('/api/instances/'+id+'/command'),
                type: "POST",
                contentType: "application/json",
                dataType: "json",
                data: JSON.stringify(command),
                success: function (data) {
                    if(options.success)
                        options.success(self,data);
                },
                error: function (args) {
                    if(options.error)
                        options.error(self,args);
                },
                complete: function (args) {
                    if(options.complete )
                        options.complete (self,args);
                }
            });
        }


    });

    return Model;
});
