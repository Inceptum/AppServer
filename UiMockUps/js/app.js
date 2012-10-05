// This is the main entry point for the App
define(['routers/home','jquery','services/notificationsListener'], function(router,jQuery,notificationsListener){
    Backbone.View.prototype.dispose = function(){
    }

    Backbone.View.prototype.close = function(){
        this.unbind();
        this.undelegateEvents();
        $(this.el).html('');
        this.dispose();
    }


    var init = function(){
        notificationsListener.init();
		this.router = new router();
	};
	
	return { init: init};
});
