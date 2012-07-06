define([
    'jQuery',
    'Underscore',
    'Backbone'
    , 'models/configuration'
], function ($, _, Backbone, configurationsModel) {

    var configurationsCollection = Backbone.Collection.extend({

        model:configurationsModel,

        url:'/configurations',

        comparator:function (cfg) {
            return cfg.get("name").toLowerCase();
        }
    });

    return configurationsCollection;
});
