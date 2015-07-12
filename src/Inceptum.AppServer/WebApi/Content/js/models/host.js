define(['jquery', 'backbone', 'underscore','context'], function($, Backbone, _,context){
	var Model = Backbone.Model.extend({
        url:context.httpUrl('/api/host')

    });
	
	return Model;
});
