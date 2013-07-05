define(['jquery', 'backbone', 'underscore','context'], function($, Backbone, _,context){

    var Model = Backbone.Model.extend({
        urlRoot: context.httpUrl("/api/instance"),
        initialize: function() {
            _.bindAll(this, "command","start","stop");
        },
        idAttribute: "Id",
        start:function(options){
            this.action("start",options);
        },
        stop:function(options){
            this.action("stop",options);
        },
        action:function(action,options){
            var id = this.id;
            var self=this;
            $.ajax({
                url: context.httpUrl('/api/instance/'+id+'/'+action),
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
                url: context.httpUrl('/api/instance/'+id+'/'+command),
                type: 'POST',
                success: function (data) {
                    if(options.success)
                        options.success(self,data);
                },
                error: function (args) {
                    if(options.error)
                        options.error(self,args);
                }
            });
        }


    });

    return Model;
});
