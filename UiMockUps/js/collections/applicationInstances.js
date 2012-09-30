define(['jquery', 'backbone', 'underscore','models/applicationInstance','context'], function($, Backbone, _, ApplicationInstanceModel,context){
    var Collection = Backbone.Collection.extend({
        model:ApplicationInstanceModel,
        url:context.httpUrl('/api/instances')
    });

    return new Collection();
});
