let map;
let draw;
let vectorSource;
const drawnFeatures = [];

document.addEventListener("DOMContentLoaded", function () {
    const osmLayer = new ol.layer.Tile({
        source: new ol.source.OSM()
    });

    const esriSatelliteLayer = new ol.layer.Tile({
        source: new ol.source.XYZ({
            url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}'
        })
    });

    const esriTerrainLayer = new ol.layer.Tile({
        source: new ol.source.XYZ({
            url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{z}/{y}/{x}'
        })
    });

    vectorSource = new ol.source.Vector({ wrapX: false });
    const vectorLayer = new ol.layer.Vector({ source: vectorSource });

    map = new ol.Map({
        target: 'map',
        layers: [osmLayer, vectorLayer],
        view: new ol.View({
            center: ol.proj.fromLonLat([32.836943, 39.925054]),
            zoom: 17
        })
    });

    const mapTypeSelect = document.getElementById("mapType");
    mapTypeSelect.addEventListener("change", function () {
        map.getLayers().removeAt(0);
        const selected = mapTypeSelect.value;
        if (selected === "osm") map.getLayers().insertAt(0, osmLayer);
        else if (selected === "satellite") map.getLayers().insertAt(0, esriSatelliteLayer);
        else if (selected === "terrain") map.getLayers().insertAt(0, esriTerrainLayer);
    });

    vectorSource.on('addfeature', function (event) {
        drawnFeatures.push(event.feature);
    });

    const drawTypeSelect = document.getElementById("drawType");
    drawTypeSelect.addEventListener("change", function () {
        if (draw) map.removeInteraction(draw);

        const selectedType = drawTypeSelect.value;

        if (selectedType !== "None") {
            draw = new ol.interaction.Draw({
                source: vectorSource,
                type: selectedType
            });

            map.addInteraction(draw);
        }
    });

    document.getElementById("gotoCoord").addEventListener("click", function () {
        const lat = parseFloat(document.getElementById("lat").value);
        const lon = parseFloat(document.getElementById("lon").value);

        if (isNaN(lat) || isNaN(lon)) {
            alert("Geçerli bir enlem ve boylam giriniz.");
            return;
        }

        const view = map.getView();
        const newCenter = ol.proj.fromLonLat([lon, lat]);
        view.animate({
            center: newCenter,
            zoom: 17,
            duration: 1000
        });
    });

    document.getElementById("clearDrawings").addEventListener("click", function () {
        vectorSource.clear();
        drawnFeatures.length = 0;
    });

    document.getElementById("undoDrawing").addEventListener("click", function () {
        if (drawnFeatures.length > 0) {
            const last = drawnFeatures.pop();
            vectorSource.removeFeature(last);
        } else {
            alert("Geri alınacak bir işlem yok.");
        }
    });

    document.getElementById("downloadDrawings").addEventListener("click", function () {
        const features = vectorSource.getFeatures();

        if (!features.length) {
            alert("İndirilecek bir çizim yok.");
            return;
        }

        const geojsonFormat = new ol.format.GeoJSON();
        const geojsonStr = geojsonFormat.writeFeatures(features, {
            featureProjection: 'EPSG:3857',
            dataProjection: 'EPSG:4326'
        });

        const blob = new Blob([geojsonStr], { type: "application/vnd.geo+json" });
        const url = URL.createObjectURL(blob);

        const a = document.createElement("a");
        a.href = url;
        a.download = "cizimler.geojson";
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    });

    document.getElementById("gotoPlace").addEventListener("click", function () {
        const place = document.getElementById("placeName").value.trim();

        if (!place) {
            alert("Bir yer adı giriniz.");
            return;
        }

        fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(place)}`)
            .then(response => response.json())
            .then(data => {
                if (data.length === 0) {
                    alert("Yer bulunamadı.");
                    return;
                }

                const lat = parseFloat(data[0].lat);
                const lon = parseFloat(data[0].lon);

                const view = map.getView();
                const newCenter = ol.proj.fromLonLat([lon, lat]);
                view.animate({
                    center: newCenter,
                    zoom: 14,
                    duration: 1000
                });
            })
            .catch(err => {
                console.error(err);
                alert("Bir hata oluştu.");
            });
    });

    document.getElementById("saveDrawings").addEventListener("click", function () {
        const features = vectorSource.getFeatures();

        if (!features.length) {
            alert("Kaydedilecek çizim yok.");
            return;
        }

        const wktFormat = new ol.format.WKT();

        const wktList = features.map(f => {
            return wktFormat.writeFeature(f, {
                dataProjection: 'EPSG:4326',
                featureProjection: 'EPSG:3857'
            });
        });

        fetch('/Home/SaveDrawings', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ wktList })
        })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    alert("Çizimler kaydedildi!");
                } else {
                    alert("Hata: " + data.message);
                }
            })
            .catch(err => {
                console.error(err);
                alert("Bir hata oluştu.");
            });
    });




});


