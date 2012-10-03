define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/instanceRow.html'],
    function($, Backbone, _, template){
        var View = Backbone.View.extend({
            tagName:"tr",
            initialize: function(){
                _.bindAll(this, "render");
                this.model.bind('change', this.render);
            },
            events:{
                "click .start":"start",
                "click .stop":"stop",
                "click .delete":"delete"
            },


            'delete' : function(e){
                e.preventDefault();
                $(this.el).find(".actions button").attr("disabled", "disabled");
                this.trigger('destroy',this.model,this);
            },
/*
            start:function(e){
                e.preventDefault();
                $(this.el).find(".actions button").attr("disabled", "disabled");
                this.model.start();
            },
            stop:function(e){
                e.preventDefault();
                $(this.el).find(".actions button").attr("disabled", "disabled");
                this.model.stop();
            },
*/

            stop:function(e){
                e.preventDefault();
                $(this.el).find(".actions button").attr("disabled", "disabled");
                this.trigger('stop',this.model,this);
            },
            start:function(e){
                e.preventDefault();
                $(this.el).find(".actions button").attr("disabled", "disabled");
                this.trigger('start',this.model,this);
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

                return this;
            },
            'dispose':function(){
                this.model.unbind('change', this.render);
            }
        });

        return View;
    });
