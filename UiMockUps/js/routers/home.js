define([
	'jquery', 
	'backbone', 
	'underscore',
    'collections/applications',
    'collections/applicationInstances',
    'models/host',
	'views/header',
    'views/serverSideBar',
	'views/serverStatus',
	'views/serverLog',
    'views/appsSideBar',
    'views/applicationInstance',
    'views/application',
    'views/instanceEdit'
],
function($, Backbone, _,Applications,Instances,HostModel, HeaderView,
         ServerSideBarView, ServerStatusView, ServerLogView,
         AppsSideBarView, ApplicationInstanceView, AppView,
         InstanceEditView){
	var Router = Backbone.Router.extend({
		initialize: function(){
            //TODO: need to have collections injected as singletons and react on events to render views
            this.currentViews=[];
			this.headerView = HeaderView;
            this.apps = Applications;
            this.apps.fetch({async:false});
            this.instances=Instances;
            //this.instances.fetch({async:false});
            this.headerView.render();
            this.hostModel = new HostModel();
            this.hostModel.fetch({async:false});
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
			'applications': 'applications',
			'applications/:app': 'applications',
			'applications/:app/instances/create': 'createInstance',
			'instances/:name': 'instance'
		},
		'serverStatus': function(){
            this.showViews([
                new ServerStatusView({model:this.hostModel,instances:this.instances}),
                new ServerSideBarView({active:"serverStatus"})
            ]);
            this.headerView.selectMenuItem("server");
		},
		'serverLog': function(){
            this.showViews([
                new ServerLogView(),
                new ServerSideBarView({active:"serverLog"})
            ]);
            this.headerView.selectMenuItem("server");
		},
        'instance': function(name){
            var model = this.instances.get(name);

            var active;
            var application;
            if (model) {
                active = model.get("ApplicationId");
                application = this.apps.get(model.get("ApplicationId"));
            }
            this.showViews([
                new AppsSideBarView({collection:this.apps, active:active}),
                new InstanceEditView({application:application,model:model})
            ]);
            this.headerView.selectMenuItem("applications");
		},
		'applications': function(app){
            var views=[new AppsSideBarView({collection:this.apps, active:app})];
            var application;
            if(app)
               application = this.apps.get(app);
            if(application)
                views.push(new AppView({model:application,instances:this.instances}));

            this.showViews(views);
            this.headerView.selectMenuItem("applications");
		},
		'createInstance': function(app){
            var application;
            if (app)
                application = this.apps.get(app);
            var views=[
                new AppsSideBarView({collection:this.apps, active:app}),
                new InstanceEditView({application:application})
            ];
            this.showViews(views);
            this.headerView.selectMenuItem("applications");
		}
	});

	return Router;
});
