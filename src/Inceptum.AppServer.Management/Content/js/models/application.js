define(['jquery', 'backbone', 'underscore','collections/applicationVersions'], function($, Backbone, _,ApplicationVersions){

    var Model = Backbone.Model.extend({


        initialize: function() {
            this.versions = new ApplicationVersions(this.get('Versions'), {application: this});
          //  this.items.bind('change', this.save);
        },
        url:function () {
            return "/api/applications/" + this.get("Configuration") + "/" + this.id;
        },
        idAttribute: "Name"

    });

    return Model;
});
