define(['jquery', 'backbone', 'underscore','models/instance','context'], function($, Backbone, _, instanceModel,context){
    var Collection = Backbone.Collection.extend({
        model:instanceModel,
        url:context.httpUrl('/api/instances'),
    });

    return new Collection();
});
