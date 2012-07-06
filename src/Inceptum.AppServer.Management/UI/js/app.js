// Filename: app.js
define([
    'jQuery',
    'Underscore',
    'Backbone',
    'router', // Request router.js
    'collections/configurations'
], function ($, _, Backbone, Router, configurationsCollection) {

    var initialize = function () {

        Backbone.View.prototype.close = function () {
            console.log('Closing view ' + this);
            if (this.beforeClose) {
                this.beforeClose();
            }
            this.remove();
            this.unbind();
        };

        var configurations = new configurationsCollection();
        configurations.fetch({
            success:function () {
                Router.initialize({configurations:configurations});
            }
        })
    }

    return {
        initialize:initialize
    };
});
