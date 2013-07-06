define(['jquery', 'backbone', 'underscore','collections/applicationVersions','backbone.composite.keys'], function($, Backbone, _,ApplicationVersions){

    var Model = Backbone.Model.extend({


        initialize: function() {
            this.versions = new ApplicationVersions(this.get('Versions'), {application: this});
          //  this.items.bind('change', this.save);
        },
        url:function () {
            return "/api/applications/" + this.get("Configuration") + "/" + this.id;
        },
        idAttribute: ['Vendor', "Name"]
    });

    return Model;
});
