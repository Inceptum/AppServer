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
                 if(model.attributes.Vendor == this.activeItem.Vendor&& model.attributes.Name==this.activeItem.Name)
                     this.activeItem=null;
                 this.render();
            },
            render: function(){
                var grouped=_.groupBy(this.applications.toJSON(),function(a){return a.Vendor;})
                var sorted=[];
                for ( var vendor in grouped ){
                    sorted.push({name:vendor,apps:grouped[vendor]});
                }
                sorted=_.sortBy(sorted, function(v){ return v.name; });

                                                              console.log(sorted);
                this.template = _.template( template, { model: sorted } );
                $(this.el).html(this.template);
                $(this.el).find('*[data-name="'+this.activeItem.Name+'"][data-vendor="'+this.activeItem.Vendor+'"]').addClass("active");
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
