// This is the main entry point for the App
define(['routers/home','jquery','services/notificationsListener'], function(Router,jQuery,notificationsListener){
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
        var router = new Router();
        this.router = router;
        Backbone.View.prototype.navigate = function (loc) {
            router.navigate(loc, true);
        };
	};
	
	return { init: init};
});
