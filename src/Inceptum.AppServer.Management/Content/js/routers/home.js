define([
	'jquery', 
	'backbone', 
	'underscore',
    'collections/applications',
    'collections/instances',
    'collections/configurations',
    'models/host',
    'models/bundle',
	'views/header',
    'views/serverSideBar',
	'views/serverStatus',
	'views/serverLog',
    'views/appsSideBar',
    'views/instance',
    'views/application',
    'views/instanceEdit',
    'views/configuration',
    'views/bundle',
    'views/configurationsSideBar'
],
function($, Backbone, _,Applications,Instances,Configurations,HostModel,BundleModel, HeaderView,
         ServerSideBarView, ServerStatusView, ServerLogView,
         AppsSideBarView, instanceView, AppView,
         InstanceEditView,ConfigurationView,BundleView,ConfigurationsSideBarView){
	var Router = Backbone.Router.extend({
		initialize: function(){
            this.hostModel = new HostModel();
            this.hostModel.fetch({async:false});
            Configurations.fetch({async:false});

            //TODO: need to have collections injected as singletons and react on events to render views
            this.currentViews=[];
			this.headerView = new HeaderView({model:this.hostModel});
            this.apps = Applications;
            this.instances=Instances;
            this.headerView.render();
			Backbone.history.start();
        },
        showViews:function(views){
            if(!( Object.prototype.toString.call( views ) === '[object Array]' )){
                views=[views];
            }
            _.each(this.currentViews.reverse(),function(v){v.close();});
            this.currentViews=[];

            var self=this;
            _.each(views,function(v){
                v.render();
                self.currentViews.push(v);
            });
        },
		routes: {
			'': 'serverStatus',
			'serverStatus': 'serverStatus',
			'serverLog': 'serverLog',
			'serverLog/:instanceName': 'serverLog',
			'applications': 'applications',
			'applications/:vendor/:app': 'applications',
			'applications/:vendor/:app/instances/create': 'createInstance',
			'instances/:name': 'instance',
			'configurations': 'configurations',
			'configurations/:config': 'configurations',
			'configurations/:config/bundles/:bundle': 'bundle',
			'configurations/:config/create': 'createBundle',
			'configurations/:config/:parent/create': 'createBundle'
		},
		'serverStatus': function(){
            this.showViews([
                new ServerStatusView({model:this.hostModel,instances:this.instances}),
                new ServerSideBarView({active:"serverStatus"})
            ]);
            this.headerView.selectMenuItem("server");
		},
		'serverLog': function(instanceName){
            this.showViews([
                new ServerLogView({ selectedInstance: instanceName }),
                new ServerSideBarView({active:"serverLog"})
            ]);
            this.headerView.selectMenuItem("server");
		},
        'instance': function(name){
            var model = this.instances.get(name);

            var active;
            var application;
            if (model) {
                active = {Name:model.get("ApplicationId"),Vendor:model.get("ApplicationVendor")};
                application = this.apps.get(active);
            }
            this.showViews([
                new AppsSideBarView({applications:this.apps, active:active}),
                new InstanceEditView({application:application,model:model})
            ]);
            this.headerView.selectMenuItem("applications");
		},
		'applications': function(vendor,app){
            var views=[new AppsSideBarView({applications:this.apps, active:{Vendor:vendor,Name:app}})];
            var application;
            if(app)
               //application = this.apps.get({Vendor:"InceptumSoft",Name:"TestApp"});
                application = this.apps.get({Vendor:vendor,Name:app});

            if(application)
                views.push(new AppView({model:application,instances:this.instances}));

            this.showViews(views);
            this.headerView.selectMenuItem("applications");
		},
		'createInstance': function(vendor,app){
            var application;
            if (app)
                application = this.apps.get({Vendor:vendor,Name:app});
            var views=[
                new AppsSideBarView({applications:this.apps,  active:{Vendor:vendor,Name:app}}),
                new InstanceEditView({application:application})
            ];
            this.showViews(views);
            this.headerView.selectMenuItem("applications");
		},
        'configurations': function(config){
          //  Configurations.fetch({async:false,update:true});

            var views = [
                new ConfigurationsSideBarView({active:config})
            ];
            if(Configurations.get(config)){
                views.push(
                    new ConfigurationView({model:Configurations.get(config), active:config})
                );
            }
            this.showViews(views);
            this.headerView.selectMenuItem("configurations");
        },
        'bundle': function(config,bundle){
            var views = [
                new ConfigurationsSideBarView({active:config, activeBundle:bundle})
            ];
            var c = Configurations.get(config);
            if(c){
                var b=c.getBundle(bundle);
                views.push(
                    new BundleView({model:b,configuration:c})
                );
            }
            this.showViews(views);
            this.headerView.selectMenuItem("configurations");
        },
        'createBundle': function(config,parent){
           // Configurations.fetch({async:false,update:true});
            var b = new BundleModel({Configuration:config,Parent:parent});
            var views = [
                new ConfigurationsSideBarView({active:config})
            ];
            var c = Configurations.get(config);
            if(c){
                views.push(
                    new BundleView({model:b,configuration:c})
                );
            }
            this.showViews(views);
            this.headerView.selectMenuItem("configurations");
        }
	});

	return Router;
});
