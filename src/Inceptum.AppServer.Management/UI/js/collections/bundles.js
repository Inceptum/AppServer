define([
    'jQuery',
    'Underscore',
    'Backbone',
    'models/bundle'
], function ($, _, Backbone, bundleModel) {
    var bundlesCollection = Backbone.Collection.extend({
        model:bundleModel,
        initialize:function (models,options) {
                   _(this).bindAll("find");
                    this.url=options.url;
                },
        url:function(){return this.url;}



});

return bundlesCollection;
})
;
