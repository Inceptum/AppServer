define(['jquery', 'backbone', 'underscore', 'collections/bundles'], function ($, Backbone, _, bundlesCollection) {
    var Model = Backbone.Model.extend({
        initialize:function () {
            var bundles = this.get("bundles");
            if (bundles) {
                this.bundles = new bundlesCollection(bundles);
                this.unset("bundles");
            }else{
                this.bundles = new bundlesCollection();
            }

            var model = this;
            this.bind('change', function () {
                if (model.hasChanged("bundles")) {
                    model.bundles.update(model.get("bundles"));
                    this.unset("bundles");
                }
            });
            this.loadedFromServer = false;
        },
        parse: function (response) {
            this.loadedFromServer = true;
            return response;
        },
        isNew: function () {
            //backbone determines whether the model is new as id==null. Since configuration uses id equal to name, backbone approach does not work
            return !this.loadedFromServer;
        },
        idAttribute:"name",
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
            var parentId = model.get("parent");
            var parent;
            if(!parentId)
                parent = this;
            else
                parent =this.getBundle(parentId);
            console.log(model);
            if (parent)
                return parent.bundles.create(model, options);
            else
                throw new Error("Can't create bundle, provided parent does not exist");

        }

    });
    return Model;
});