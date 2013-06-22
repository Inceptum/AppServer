define(['jquery', 'backbone', 'underscore', 'collections/bundles'], function ($, Backbone, _, BundlesCollection) {
    var Model = Backbone.Model.extend({
        initialize:function () {
            var bundles = this.get("Bundles");
            if (bundles) {
                this.bundles = new BundlesCollection(bundles);
                this.unset("bundles");
            }else{
                this.bundles = new BundlesCollection();
            }

            var model = this;
            this.bind('change', function () {
                if (model.hasChanged("Bundles")) {
                    model.bundles.update(model.get("Bundles"));
                    this.unset("Bundles");
                }
            });
        },
        idAttribute:"Name",
        getBundle:function (id) {
            var path;
            var parts = id.split(".");
            var bundle;
            var bundles = this.bundles;
            _.each(parts, function (part) {
                if (path)path = path + "." + part;
                else path = part;
                bundle = bundles.get(path);
                if (bundle == undefined)
                    return bundle;
                bundles = bundle.bundles;
            });
            return bundle;
        },
        createBundle:function (model, options) {
            var parentId = model.get("Parent");
            var parent;
            if(!parentId)
                parent = this;
            else
                parent =this.getBundle(parentId);
            if (parent)
                return parent.bundles.create(model, options);
            else
                throw new Error("Can't create bundle, provided parent does not exist");

        }

    });
    return Model;
});