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
], function ($, _, Backbone, mainHomeView, menuView, messageView, configurationListView, configurationView, bundleView, bundleModel, messageModel) {
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
            $(".logo").html("Inceptum AppServer");
            this.reset();
            this.showView("#page", new configurationListView({collection:configurations }));
        },

        showConfiguration:function (configuration) {
            if (configuration)
                this.selectedConfiguration = configuration;
            else {
                this.selectedBundle = null;
                this.navigate('configurations/' + this.selectedConfiguration, {trigger:true});
                return;
            }
            var self = this,
                config = configurations.get(this.selectedConfiguration);

            if (!config) {
                this.showMessage("message", "Configuration " + configuration + " not found!");
                this.navigate('configurations', {trigger:true});
                return;
            }

            $(".logo").html(config.get("name"));
            console.log(config.get("bundlesmap"));
            config.fetch({success:function () {
                console.log(config.get("bundlesmap"));
                self.showView("#page", new configurationView({model:config }));

                if (self.selectedBundle) {
                    self.showBundle(configuration, self.selectedBundle);
                }
            }})
        },

        showBundle:function (configuration, bundle) {
            this.closeBundleView();

            this.selectedBundle = bundle;

            if (this.selectedConfiguration === configuration) {
                var currentConfiguration = configurations.get(this.selectedConfiguration),
                    currentBundle = currentConfiguration.bundles.get(this.selectedBundle);
                if (!currentBundle) {
                    var content = "{\r\n}",
                        parents = [],
                        p = bundle.indexOf(".");
                    while (p != -1) {
                        parents.push(bundle.substr(0, p));
                        p = bundle.indexOf(".", p + 1);
                    }

                    var b;
                    for (i = parents.length - 1; i >= 0; i-- && !b) {
                        b = currentConfiguration.bundles.get(parents[i]);
                    }

                    if (b)
                        content = b.get("Content");

                    currentBundle = new bundleModel({id:this.selectedBundle, Configuration:this.selectedConfiguration, Content:content});
                    currentBundle.IsNewBundle = true;
                }
                this.bundleView = new bundleView({model:currentBundle, configuration:currentConfiguration});
                $(".bundle").html(this.bundleView.render().el);
            }
            else {
                this.showConfiguration(configuration);
            }
        },

        defaultAction:function (actions) {
            this.reset();
            // We have no matching route, lets display the home page
            this.showView("#page", mainHomeView);
        },

        reset:function () {
            this.selectedConfiguration = null;
            this.selectedBundle = null;
        },

        closeBundleView:function () {
            if (this.bundleView) {
                this.bundleView.close();
                this.bundleView = null;
            }
        },

        createBundle:function (name) {
            var fullName = this.selectedBundle ? this.selectedBundle + "." + name : name;
            this.navigate('configurations/' + this.selectedConfiguration + '/' + fullName, {trigger:true});
        },

        showMessage:function (severity, text) {
            var message = new messageModel({severity:severity, text:text});
            $("#message").html(new messageView({model:message}).render().el);
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
            console.log("Router initalized");
            _.bindAll(this, "defaultAction", "showBundle", "showConfiguration", "showConfigurations");
        }
    });

    var initialize = function (options) {
        configurations = options.configurations;
        window.Router = new AppRouter; //Global
        Backbone.history.start();
    };

    return {
        initialize:initialize
    };
});
