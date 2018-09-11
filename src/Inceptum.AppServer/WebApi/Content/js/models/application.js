define(['jquery', 'backbone', 'underscore','collections/applicationVersions','backbone.composite.keys'], function($, Backbone, _,ApplicationVersions){

    var Model = Backbone.Model.extend({


        initialize: function() {
            _(this).bindAll('resetVersions');
            this.resetVersions();
            this.bind("change", this.resetVersions);
        },
        url:function () {
            return "/api/applications/" + this.get("configuration") + "/" + this.id;
        },
        idAttribute: ['vendor', "name"],
        resetVersions: function () {
            this.versions = new ApplicationVersions(this.get('versions'), {application: this});
        }


});

    return Model;
});
