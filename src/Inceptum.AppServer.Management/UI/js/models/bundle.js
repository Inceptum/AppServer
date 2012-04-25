define([
    'Underscore',
    'Backbone'
], function (_, Backbone) {

    var bundleModel = Backbone.Model.extend({

      /*  sync:function(){
            console.log('saving...');
            Backbone.sync.apply(Backbone, arguments);
        },*/

        initialize:function () {
            var bundlesCollection=require("collections/bundles");
            if (!_.isUndefined(this.get("subbundles"))) {
                this.set({subbundles : new bundlesCollection(this.get("subbundles"))});
            }
        },
        url:function () {
            console.log("http://localhost:9223/configurations/" + this.get("Configuration")+"/"+this.get("id"));
                    return "http://localhost:9223/configurations/" + this.get("Configuration")+"/"+this.get("id");
                }

    });

    return bundleModel;

});
