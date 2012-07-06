define([
    'jQuery',
    'Underscore',
    'Backbone',
    'models/bundle'
], function ($, _, Backbone, bundleModel) {

    var bundlesCollection = Backbone.Collection.extend({
        model:bundleModel,

        initialize:function (models, options) {
            this.configuration = options.configuration;
        },

        url:function () {
            return this.configuration.url() + "/bundles";
        }
    });

    return bundlesCollection;
});
