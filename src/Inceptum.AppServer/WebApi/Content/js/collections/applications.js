define(['jquery', 'backbone', 'underscore','models/application','context'], function($, Backbone, _, applicationModel,context){
    var Collection = Backbone.Collection.extend({
        model:applicationModel,
        idAttribute: "version",
        url:context.httpUrl('/api/applications')
    });

    return new Collection();
});
