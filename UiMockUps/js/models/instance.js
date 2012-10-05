define(['jquery', 'backbone', 'underscore','context'], function($, Backbone, _,context){

    var Model = Backbone.Model.extend({
        urlRoot: context.httpUrl("/api/instance"),
        initialize: function() {
            _.bindAll(this, "command","start","stop");
        },
        idAttribute: "Id",
        start:function(options){
            this.command("start",options);
        },
        stop:function(options){
            this.command("stop",options);
        },
        command:function(action,options){
            var id = this.id;
           // console.log(action+' '+id+' sent');
            var self=this;
            $.ajax({
                url: context.httpUrl('/api/instance/'+id+'/'+action),
                type: 'POST',
                success: function () {
                    //console.log(action+' '+id+' complete');
                    options.success(self);
                },
                error: function (args) {
                    //console.log(action+' '+id+' failed:');
                    options.error(self,args);
                }
            });
        }

    });

    return Model;
});
