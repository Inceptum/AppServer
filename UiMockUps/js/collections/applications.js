define(['jquery', 'backbone', 'underscore','models/application','context'], function($, Backbone, _, applicationModel,context){
    var Collection = Backbone.Collection.extend({
        model:applicationModel,
        idAttribute: "Version",
        url:context.httpUrl('/api/applications')
    });

    return new Collection();
});
