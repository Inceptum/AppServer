define(['jquery', 'backbone', 'underscore','models/instance','context'], function($, Backbone, _, instanceModel,context){
    var Collection = Backbone.Collection.extend({
        model:instanceModel,
        url:context.httpUrl('/api/instances'),
        comparator: function (config) {
            //console.log([config.get("Environment"),config.get("Name")]);
            return [config.get("environment"),config.get("name")];
        }

    });

    return new Collection();
});
