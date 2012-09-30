define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/applicationInstanceRow.html','context'],
    function($, Backbone, _, template,context){
        var View = Backbone.View.extend({
            tagName:"tr",
            initialize: function(){
            },
            events:{
                "click .start":"start",
                "click .stop":"stop",
                "click .delete":"delete"
            },
            delete:function(e){
                e.preventDefault();
                $(this.el).find(".actions button").attr("disabled", "disabled");
                this.model.destroy();
            },
            start:function(e){
                e.preventDefault();
                $(this.el).find(".actions button").attr("disabled", "disabled");

                $.ajax({
                    url: context.httpUrl('/api/instance/'+this.model.id+'/start'),
                    type: 'POST',
                    //data: ({ id: theId }),
                    success: function () { console.log('start sent'); },
                    error: function (args) { console.log('start failed:'+args); }
                });
            },
            stop:function(e){
                e.preventDefault();
                $(this.el).find(".actions button").attr("disabled", "disabled");
                $.ajax({
                    url:context.httpUrl('/api/instance/'+this.model.id+'/stop'),
                    type: 'POST',
                    //data: ({ id: theId }),
                    success: function () { console.log('stop sent'); },
                    error: function () { console.log('stop failed'); }
                });
            },

            render: function(){
                this.template = _.template( template, { model: this.model.toJSON() } );
                $(this.el).html(this.template);
                $(this.el).find(".actions button").attr("disabled", "disabled");

                if(this.model.get("Status")=="Started")
                {
                    $(this.el).find(".stop").removeAttr("disabled");
                    $(this.el).find(".delete").removeAttr("disabled");
                }
                if(this.model.get("Status")=="Stopped")
                {
                    $(this.el).find(".start").removeAttr("disabled");
                    $(this.el).find(".delete").removeAttr("disabled");
                }
                console.log("rendered "+this.model.id+"in "+this.model.get("Status")+": start.disabled="+ $(this.el).find(".start").attr("disabled")+"  stop.disabled="+ $(this.el).find(".stop").attr("disabled"));

                return this;
            }
        });

        return View;
    });
