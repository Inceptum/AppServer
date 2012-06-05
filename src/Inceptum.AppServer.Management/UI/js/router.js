// Filename: router.js
define([
    'jQuery',
    'Underscore',
    'Backbone' ,
    'views/home/main',
    'views/home/menu',
    'views/home/message',
    'views/configurations/list',
    'views/configurations/configuration',
    'views/bundles/bundle',
    'models/bundle',
    'models/message'
], function ($, _, Backbone, mainHomeView, menuView,messageView, configurationListView, configurationView, bundleView, bundleModel, messageModel) {
    var AppRouter = Backbone.Router.extend({

        routes:{
            // Define some URL routes
            'configurations':'showConfigurations',
            'configurations/:configuration':'showConfiguration',
            'configurations/:configuration/:bundle':'showBundle',
            // Default
            '*actions':'defaultAction'
        },

        showConfigurations:function () {
            $(".logo").html("");
            this.selectedConfiguration  = null;
            this.selectedBundle= null;
            this.showView("#page", new configurationListView({collection:configurations}));
        },
        showConfiguration:function (configuration) {
            if(configuration)
                this.selectedConfiguration  = configuration;
            else{
                this.selectedBundle=null;
                this.navigate('configurations/' + this.selectedConfiguration, {trigger:true});
            }
            var conf = configurations.get(this.selectedConfiguration);
            var self=this;
            $(".logo").html(conf.get("name"));
            console.log(conf.get("bundlesmap"));
            conf.fetch({success:function(){
                console.log(conf.get("bundlesmap"));
                self.showView("#page", new configurationView({model:conf }));

                if(self.selectedBundle){
                    self.showBundle(configuration,self.selectedBundle);
                }
            }})
        },
        createBundle:function(name){
            this.navigate('configurations/' + this.selectedConfiguration+'/'+name, {trigger:true});
        },
        showMessage:function(severity,text){
            var message=new messageModel({severity:severity,text:text});
            $("#message").html(new messageView({model:message}).render().el);
        },
        showBundle:function (configuration, bundle) {

            this.selectedBundle=bundle;

            if(this.selectedConfiguration===configuration){
                var currentConfiguration = configurations.get(this.selectedConfiguration);
                var currentBundle =currentConfiguration.bundles.get(this.selectedBundle);
                if(!currentBundle){
                    var content="{}";
                    var p=bundle.indexOf(".");
                    var parents=[];
                    while (p!=-1){
                        parents.push(bundle.substr(0,p));
                        p=bundle.indexOf(".",p+1);
                    }

                    var b;
                    for(i=parents.length-1;i>=0;i-- && !b){
                        b=currentConfiguration.bundles.get(parents[i]);
                    }

                    if(b)
                        content= b.get("Content");

                    currentBundle=new bundleModel({id:this.selectedBundle,Configuration:this.selectedConfiguration, Content:content});
                    currentBundle.IsNewBundle=true;
                }
               $(".bundle").html(new bundleView({model:currentBundle, configuration:currentConfiguration, router:this}).render().el);
            }
            else{
                this.showConfiguration(configuration);
            }
        },


        defaultAction:function (actions) {
            this.selectedConfiguration  = null;
            this.selectedBundle  = null;
            // We have no matching route, lets display the home page
            this.showView("#page", mainHomeView);
        },


        showView:function (selector, view) {
            if (this.currentView)
                this.currentView.close();
            //$("#message").html('');
            $(selector).html(view.render().el);
            this.currentView = view;
            $(".menu_main").html(menuView.render().el);
            return view;
        },
        initialize:function () {
            console.log("router initalized")
            _.bindAll(this, "defaultAction", "showBundle", "showConfiguration", "showConfigurations");
        }
    });

    var initialize = function (options) {
        configurations = options.configurations;
        var app_router = new AppRouter;
        Backbone.history.start();
    };
    return {
        initialize:initialize
    };
});
