define([
    'Underscore',
    'Backbone'
], function (_, Backbone) {

    var bundleModel = Backbone.Model.extend({

        url:function () {
            console.log("/configurations/" + this.get("Configuration") + "/" + this.id);
            return "/configurations/" + this.get("Configuration") + "/" + this.id;
        }

    });

    return bundleModel;
});
