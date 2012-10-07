define(['jquery', 'backbone', 'underscore','models/configuration','context'], function($, Backbone, _,ConfigurationModel,context){
    var Collection = Backbone.Collection.extend({
        model: ConfigurationModel,
        url:context.httpUrl('/api/configurations')
    });
    return new Collection();
});