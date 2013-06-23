define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/appsSideBar.html','context','views/alerts'],
    function($, Backbone, _, template,context,alerts){
        var View = Backbone.View.extend({
            el:'#sidebar',
            initialize: function(){
                this.applications=this.options.applications;
                this.activeItem=this.options.active;
                _(this).bindAll('reset','remove');
                this.applications.bind('remove', this.remove);
                this.applications.bind('add', this.reset);
                this.applications.bind('reset', this.reset);
            },
            events:{
                "click .rediscover":"rediscoverApps"
            },
            reset:function(model){
                 this.render();
            },
            remove:function(model){
                 if(model.id == this.activeItem)
                     this.activeItem=null;
                 this.render();
            },
            render: function(){
                this.template = _.template( template, { model: this.applications.toJSON() } );
                $(this.el).html(this.template);
                $(this.el).find('*[data-id="'+this.activeItem+'"]').addClass("active");
            },
            rediscoverApps:function(e,sender){
                var self=this;
                $.ajax({
                    url: context.httpUrl('/api/applications/rediscover'),
                    type: 'POST',
                    success: function () {
                    },
                    error: function (args) {
                        console.log(args);
                        var error="";
                        if(args.responseText)
                            error=JSON.parse(args.responseText).Error;
                        alerts.show({
                            type:"error",
                            text:"Failed to rediscover applications"+error
                        });
                    }
                });
            },
            'dispose':function(){
            }
        });

        return View;
    });
