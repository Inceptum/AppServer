define(['jquery', 'backbone', 'underscore','collections/applicationVersions','backbone.composite.keys'], function($, Backbone, _,ApplicationVersions){

    var Model = Backbone.Model.extend({


        initialize: function() {
            _(this).bindAll('resetVersions');
            this.resetVersions();
            this.bind("change", this.resetVersions);
        },
        url:function () {
            return "/api/applications/" + this.get("Configuration") + "/" + this.id;
        },
        idAttribute: ['Vendor', "Name"],
        resetVersions: function () {
            this.versions = new ApplicationVersions(this.get('Versions'), {application: this});
        }


});

    return Model;
});
