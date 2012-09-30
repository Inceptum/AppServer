// This is the main entry point for the App
define(['routers/home','jquery','services/notificationsListener'], function(router,jQuery,notificationsListener){
    Backbone.View.prototype.close = function(){
        this.unbind();
        this.undelegateEvents();
        $(this.el).html('');
        this.dispose();
        console.log(arguments.callee.name+" closed");
    },
    Backbone.View.prototype.dispose = function(){
    }

    var init = function(){
        notificationsListener.init();
		this.router = new router();
	};
	
	return { init: init};
});
