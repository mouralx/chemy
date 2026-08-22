(function () {
    "use strict";

    var bootstrapElement = document.getElementById("chemy-bootstrap");
    if (!bootstrapElement) return;

    var bootstrap = {};
    try {
        bootstrap = JSON.parse(bootstrapElement.textContent || "{}");
    } catch (error) {
        console.error("Chemy bootstrap data could not be parsed.", error);
    }

    var state = {
        currentInput: bootstrap.formula || "H2O",
        moleculeName: bootstrap.moleculeName || bootstrap.formula || "Molecule",
        chemicalFormula: bootstrap.chemicalFormula || bootstrap.formula || "",
        molecularWeight: bootstrap.molecularWeight || 0,
        totalAtomCount: bootstrap.totalAtomCount || 0,
        vseprShape: bootstrap.vseprShape || "Unknown",
        idealBondAngleDegrees: bootstrap.idealBondAngleDegrees || 0,
        elementsPresent: bootstrap.elementsPresent || [],
        functionalGroups: bootstrap.functionalGroups || [],
        pdbContent: bootstrap.pdbContent || "",
        xyzContent: bootstrap.xyzContent || "",
        molContent: bootstrap.molContent || "",
        planarPdbContent: bootstrap.planarPdbContent || "",
        planarXyzContent: bootstrap.planarXyzContent || "",
        planarMolContent: bootstrap.planarMolContent || "",
        skeletalSvgContent: bootstrap.skeletalSvgContent || "",
        visualMode: "3d",
        spatialMode: "3d",
        showLabels: false,
        spinning: false,
        viewer: null
    };

    var compounds = [
        { name: "Water", input: "H2O", formula: "H₂O", group: "Inorganic" },
        { name: "Methane", input: "CH4", formula: "CH₄", group: "Hydrocarbon" },
        { name: "Carbon dioxide", input: "CO2", formula: "CO₂", group: "Inorganic" },
        { name: "Aspirin", input: "CC(=O)Oc1ccccc1C(=O)O", formula: "C₉H₈O₄", group: "Pharmaceutical" },
        { name: "Caffeine", input: "Cn1c(=O)n(C)c2ncn(C)c2c1=O", formula: "C₈H₁₀N₄O₂", group: "Natural product" },
        { name: "Paracetamol", input: "CC(=O)NC1=CC=C(C=C1)O", formula: "C₈H₉NO₂", group: "Pharmaceutical" },
        { name: "Ibuprofen", input: "CC(C)CC1=CC=C(C=C1)C(C)C(=O)O", formula: "C₁₃H₁₈O₂", group: "Pharmaceutical" },
        { name: "Cocaine", input: "CN1C2CCC1C(C(=O)OC)C2OC(=O)c3ccccc3", formula: "C₁₇H₂₁NO₄", group: "Alkaloid" },
        { name: "Morphine", input: "CN1CCC23C4=C5OCOC5=C(C=C4O)C2(O)CCC3C1", formula: "C₁₇H₁₉NO₃", group: "Alkaloid" },
        { name: "PFOA", input: "PFOA", formula: "C₈HF₁₅O₂", group: "Environmental" },
        { name: "PFOS", input: "PFOS", formula: "C₈HF₁₇O₃S", group: "Environmental" },
        { name: "Glucose", input: "Glucose", formula: "C₆H₁₂O₆", group: "Biochemical" },
        { name: "ATP", input: "ATP", formula: "C₁₀H₁₆N₅O₁₃P₃", group: "Biochemical" },
        { name: "Ethanol", input: "CCO", formula: "C₂H₆O", group: "Solvent" },
        { name: "Acetone", input: "CC(=O)C", formula: "C₃H₆O", group: "Solvent" },
        { name: "Benzene", input: "c1ccccc1", formula: "C₆H₆", group: "Aromatic" },
        { name: "PET monomer", input: "O=C(O)c1ccc(cc1)C(=O)O", formula: "C₈H₆O₄", group: "Polymer" },
        { name: "Nicotine", input: "CN1CCCC1c2cccnc2", formula: "C₁₀H₁₄N₂", group: "Alkaloid" },
        { name: "Sulfuric acid", input: "H2SO4", formula: "H₂SO₄", group: "Inorganic" },
        { name: "Ammonia", input: "NH3", formula: "NH₃", group: "Inorganic" }
    ];

    var viewTitles = {
        studio: ["Workspace / Molecular studio", "Molecular studio"],
        tools: ["Workspace / Calculation tools", "Calculation tools"],
        docs: ["Knowledge / Documentation", "Documentation"],
        code: ["Knowledge / Code reference", "C# code reference"],
        scope: ["Governance / Scientific scope", "Scope and evidence"]
    };

    var documentTitles = {
        home: "Documentation home",
        started: "Getting started",
        cookbook: "Cookbook",
        api: "API reference",
        arch: "Architecture",
        science: "Scientific approach",
        credibility: "Scientific credibility report",
        benchmarks: "Scientific verification benchmarks",
        audit: "Codex audit v2.8",
        showcase: "Breakthroughs showcase"
    };

    var documentFileKeys = {
        "readme.md": "home",
        "getting_started.md": "started",
        "cookbook.md": "cookbook",
        "api_reference.md": "api",
        "architecture.md": "arch",
        "scientific_approach.md": "science",
        "scientific_credibility_report.md": "credibility",
        "scientific_verification_benchmarks.md": "benchmarks",
        "codex_audit_v2.8.md": "audit",
        "breakthroughs_showcase.md": "showcase"
    };

    function byId(id) {
        return document.getElementById(id);
    }

    function escapeHtml(value) {
        return String(value == null ? "" : value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    function humanize(value) {
        return String(value || "")
            .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
            .replace(/[_-]+/g, " ")
            .replace(/\b\w/g, function (letter) { return letter.toUpperCase(); });
    }

    function formatValue(value) {
        if (typeof value === "number") {
            if (!Number.isFinite(value)) return String(value);
            var magnitude = Math.abs(value);
            if ((magnitude > 0 && magnitude < 0.0001) || magnitude >= 1000000) return value.toExponential(4);
            return Number(value.toPrecision(7)).toString();
        }
        if (typeof value === "boolean") return value ? "Yes" : "No";
        if (value == null || value === "") return "—";
        return String(value);
    }

    function toast(message, isError) {
        var region = byId("toastRegion");
        if (!region) return;
        var item = document.createElement("div");
        item.className = "toast" + (isError ? " is-error" : "");
        item.textContent = message;
        region.appendChild(item);
        window.setTimeout(function () { item.remove(); }, 4200);
    }

    function syncThemeControl() {
        var isDark = document.documentElement.dataset.theme === "dark";
        var button = byId("themeToggleButton");
        if (!button) return;
        button.setAttribute("aria-pressed", isDark ? "true" : "false");
        button.setAttribute("aria-label", isDark ? "Switch to light theme" : "Switch to dark theme");
        byId("themeToggleIcon").textContent = isDark ? "☀" : "☾";
        byId("themeToggleLabel").textContent = isDark ? "Light" : "Dark";
        var themeMeta = document.querySelector('meta[name="theme-color"]');
        if (themeMeta) themeMeta.setAttribute("content", isDark ? "#081522" : "#0b1f36");
    }

    function toggleTheme() {
        var nextTheme = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
        document.documentElement.dataset.theme = nextTheme;
        try {
            localStorage.setItem("chemy-theme", nextTheme);
        } catch (_) {
            // Theme selection still applies for the current page when storage is unavailable.
        }
        syncThemeControl();
        if (state.viewer) {
            state.viewer.resize();
            state.viewer.render();
        }
    }

    function showView(viewName) {
        if (!viewTitles[viewName]) return;
        document.querySelectorAll("[data-view]").forEach(function (view) {
            view.classList.toggle("is-active", view.getAttribute("data-view") === viewName);
        });
        document.querySelectorAll("[data-view-target]").forEach(function (button) {
            button.classList.toggle("is-active", button.getAttribute("data-view-target") === viewName);
        });
        byId("pageEyebrow").textContent = viewTitles[viewName][0];
        byId("pageTitle").textContent = viewTitles[viewName][1];
        document.body.classList.remove("sidebar-open");
        window.scrollTo({ top: 0, behavior: "smooth" });
        if (viewName === "studio" && state.viewer) {
            window.setTimeout(function () { state.viewer.resize(); state.viewer.render(); }, 80);
        }
    }

    function activateTool(toolName) {
        showView("tools");
        byId("toolEmptyState").style.display = "none";
        document.querySelectorAll("[data-tool-panel]").forEach(function (panel) {
            panel.classList.toggle("is-active", panel.getAttribute("data-tool-panel") === toolName);
        });
        document.querySelectorAll("[data-tool-target]").forEach(function (button) {
            button.classList.toggle("is-active", button.getAttribute("data-tool-target") === toolName);
        });
        var activePanel = document.querySelector('[data-tool-panel="' + toolName + '"]');
        if (activePanel && window.innerWidth < 980) activePanel.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    function serializeForm(formId) {
        var form = byId(formId);
        var payload = {};
        if (!form) return payload;
        Array.prototype.forEach.call(form.elements, function (field) {
            if (!field.name || field.disabled || field.type === "button" || field.type === "submit") return;
            if ((field.type === "checkbox" || field.type === "radio") && !field.checked) return;
            var raw = String(field.value || "").trim();
            if (raw === "") return;
            var value = raw;
            if (field.dataset.valueType === "number") value = Number(raw);
            if (field.dataset.valueType === "integer") value = parseInt(raw, 10);
            payload[field.name] = value;
        });
        return payload;
    }

    function sanitizeSvg(svgText) {
        var parser = new DOMParser();
        var parsed = parser.parseFromString(String(svgText || ""), "image/svg+xml");
        if (parsed.querySelector("parsererror")) return "";
        parsed.querySelectorAll("script, foreignObject, iframe, object, embed").forEach(function (node) { node.remove(); });
        parsed.querySelectorAll("*").forEach(function (node) {
            Array.prototype.slice.call(node.attributes || []).forEach(function (attribute) {
                var name = attribute.name.toLowerCase();
                var value = attribute.value.toLowerCase().replace(/\s/g, "");
                if (name.indexOf("on") === 0 || value.indexOf("javascript:") === 0) node.removeAttribute(attribute.name);
            });
        });
        var svg = parsed.documentElement;
        return svg && svg.nodeName.toLowerCase() === "svg" ? new XMLSerializer().serializeToString(svg) : "";
    }

    function extractEvidence(data) {
        var pills = [];
        if (!data || typeof data !== "object") return pills;
        if (data.applicability && data.applicability.status != null) {
            var status = String(data.applicability.status);
            pills.push({ text: "Applicability: " + humanize(status), className: status.toLowerCase().replace(/\s+/g, "-") });
        }
        if (data.methodInfo && data.methodInfo.evidenceLevel != null) {
            pills.push({ text: "Evidence: " + humanize(data.methodInfo.evidenceLevel), className: "" });
        }
        if (data.diagnostics && data.diagnostics.converged != null) {
            pills.push({ text: data.diagnostics.converged ? "Numerically converged" : "Review convergence", className: data.diagnostics.converged ? "in-domain" : "boundary" });
        }
        if (data.converged != null) {
            pills.push({ text: data.converged ? "Optimization converged" : "Optimization incomplete", className: data.converged ? "in-domain" : "boundary" });
        }
        return pills;
    }

    function evidenceHtml(data) {
        var pills = extractEvidence(data);
        if (!pills.length) return "";
        return '<div class="evidence-strip">' + pills.map(function (pill) {
            return '<span class="evidence-pill ' + escapeHtml(pill.className) + '">' + escapeHtml(pill.text) + "</span>";
        }).join("") + "</div>";
    }

    function scalarEntries(data) {
        if (!data || typeof data !== "object" || Array.isArray(data)) return [];
        var excluded = /(?:format|svg|markdown|steps|points|atoms|orbitals|candidates|bands|peaks|methodInfo|applicability|diagnostics|uncertainty|minimizedMolecule|referenceUris|validationEvidence)/i;
        return Object.keys(data).filter(function (key) {
            var value = data[key];
            return !excluded.test(key) && (typeof value === "string" || typeof value === "number" || typeof value === "boolean" || value == null) && String(value == null ? "" : value).length < 160;
        }).slice(0, 12).map(function (key) { return [key, data[key]]; });
    }

    function metricsHtml(entries) {
        if (!entries.length) return "";
        return '<div class="result-metrics">' + entries.map(function (entry) {
            return '<div class="result-metric"><span title="' + escapeHtml(humanize(entry[0])) + '">' + escapeHtml(humanize(entry[0])) + '</span><strong title="' + escapeHtml(formatValue(entry[1])) + '">' + escapeHtml(formatValue(entry[1])) + "</strong></div>";
        }).join("") + "</div>";
    }

    function detailsHtml(data) {
        return '<details class="result-details"><summary>Inspect complete result contract</summary><pre>' + escapeHtml(JSON.stringify(data, null, 2)) + "</pre></details>";
    }

    function collectionHtml(items) {
        if (!Array.isArray(items) || !items.length) return "";
        var visible = items.slice(0, 30);
        return '<div class="result-collection">' + visible.map(function (item, index) {
            if (item == null || typeof item !== "object") return '<div class="collection-row"><strong>' + escapeHtml(formatValue(item)) + "</strong></div>";
            var values = scalarEntries(item).slice(0, 5);
            var heading = item.name || item.symbol || item.formula || item.moleculeName || item.rank || ("Item " + (index + 1));
            return '<div class="collection-row"><strong>' + escapeHtml(formatValue(heading)) + '</strong><span>' + values.map(function (entry) {
                return escapeHtml(humanize(entry[0])) + ": " + escapeHtml(formatValue(entry[1]));
            }).join(" · ") + "</span></div>";
        }).join("") + (items.length > visible.length ? '<p class="collection-more">Showing ' + visible.length + " of " + items.length + " records. Open the full result contract for all records.</p>" : "") + "</div>";
    }

    function renderJson(output, data, title) {
        var arrayData = Array.isArray(data) ? data : null;
        var entries = arrayData ? [["Records", arrayData.length]] : scalarEntries(data);
        var body = '<div class="result-header"><strong>' + escapeHtml(title || "Calculation result") + '</strong><span>Completed</span></div>';
        body += metricsHtml(entries);
        if (arrayData) body += collectionHtml(arrayData);
        if (data && Array.isArray(data.candidates)) body += collectionHtml(data.candidates);
        if (data && Array.isArray(data.steps)) body += collectionHtml(data.steps);
        body += evidenceHtml(data);
        body += detailsHtml(data);
        output.innerHTML = body;
    }

    function renderSvg(output, payload) {
        var raw = typeof payload === "string" ? payload : (payload && (payload.svgContent || payload.skeletalSvg || payload.svg));
        var safe = sanitizeSvg(raw);
        if (!safe) {
            renderJson(output, payload, "Structure result");
            return;
        }
        output.innerHTML = '<div class="result-header"><strong>Vector structure</strong><span>SVG</span></div><div class="svg-result">' + safe + "</div>" + (typeof payload === "object" ? detailsHtml(payload) : "");
    }

    function renderNetwork(output, data) {
        var points = data && data.points;
        if (!Array.isArray(points) || points.length < 2) {
            renderJson(output, data, "Kinetics simulation");
            return;
        }
        var width = 820;
        var height = 260;
        var inset = 24;
        var maxTime = Math.max.apply(null, points.map(function (point) { return Number(point.timeSeconds) || 0; })) || 1;
        var maxConcentration = Math.max.apply(null, points.reduce(function (all, point) {
            return all.concat([Number(point.concentrationA) || 0, Number(point.concentrationB) || 0, Number(point.concentrationC) || 0]);
        }, [])) || 1;
        function pathFor(key) {
            return points.map(function (point, index) {
                var x = inset + ((Number(point.timeSeconds) || 0) / maxTime) * (width - inset * 2);
                var y = height - inset - ((Number(point[key]) || 0) / maxConcentration) * (height - inset * 2);
                return (index ? "L" : "M") + x.toFixed(2) + " " + y.toFixed(2);
            }).join(" ");
        }
        var chart = '<svg class="network-chart" viewBox="0 0 ' + width + " " + height + '" role="img" aria-label="A to B to C concentration trajectories">' +
            '<path d="M' + inset + " " + inset + "V" + (height - inset) + "H" + (width - inset) + '" fill="none" stroke="#bcc9d6" />' +
            '<path d="' + pathFor("concentrationA") + '" fill="none" stroke="#2d66d8" stroke-width="3" />' +
            '<path d="' + pathFor("concentrationB") + '" fill="none" stroke="#7257c8" stroke-width="3" />' +
            '<path d="' + pathFor("concentrationC") + '" fill="none" stroke="#0a8c7f" stroke-width="3" /></svg>' +
            '<div class="chart-legend"><span><i style="background:#2d66d8"></i>A</span><span><i style="background:#7257c8"></i>B</span><span><i style="background:#0a8c7f"></i>C</span></div>';
        output.innerHTML = '<div class="result-header"><strong>Concentration trajectory</strong><span>RK4 simulation</span></div>' +
            metricsHtml(scalarEntries(data)) + chart + evidenceHtml(data) + detailsHtml(data);
    }

    function renderSpectroscopy(output, data) {
        var h1 = data.h1NmrPeaks || [];
        var c13 = data.c13NmrPeaks || [];
        var ir = data.irBands || [];
        function peakTags(items, field, unit) {
            return items.slice(0, 18).map(function (item) {
                return '<span class="spectrum-tag"><strong>' + escapeHtml(formatValue(item[field])) + " " + escapeHtml(unit) + '</strong><small>' + escapeHtml(item.multiplet || item.functionalGroup || item.description || "") + "</small></span>";
            }).join("");
        }
        output.innerHTML = '<div class="result-header"><strong>Spectral estimate</strong><span>Empirical result</span></div>' +
            metricsHtml([["Formula", data.formula], ["1H peaks", h1.length], ["13C peaks", c13.length], ["IR bands", ir.length]]) +
            '<div class="spectrum-section"><h4>¹H NMR</h4><div class="spectrum-tags">' + peakTags(h1, "chemicalShiftPpm", "ppm") + "</div></div>" +
            '<div class="spectrum-section"><h4>¹³C NMR</h4><div class="spectrum-tags">' + peakTags(c13, "chemicalShiftPpm", "ppm") + "</div></div>" +
            '<div class="spectrum-section"><h4>IR correlations</h4><div class="spectrum-tags">' + peakTags(ir, "waveNumberCm1", "cm⁻¹") + "</div></div>" +
            evidenceHtml(data) + detailsHtml(data);
    }

    function renderPubChem(output, data) {
        renderJson(output, data, "PubChem compound");
        var smiles = data && (data.smiles || data.canonicalSmiles);
        if (!smiles) return;
        var action = document.createElement("button");
        action.type = "button";
        action.className = "primary-button result-action";
        action.textContent = "Load structure into studio";
        action.addEventListener("click", function () {
            byId("globalMoleculeInput").value = smiles;
            loadMolecule(smiles, "Auto");
        });
        output.insertBefore(action, output.querySelector(".result-details"));
    }

    function moleculeToXyz(molecule, fallbackName) {
        if (!molecule || !Array.isArray(molecule.atoms)) return "";
        var rows = molecule.atoms.map(function (atom) {
            var symbol = atom.atom && atom.atom.element && atom.atom.element.symbol ? atom.atom.element.symbol : "C";
            var position = atom.position || {};
            return symbol + " " + (position.x || 0) + " " + (position.y || 0) + " " + (position.z || 0);
        });
        return rows.length + "\n" + (molecule.name || fallbackName || "Chemy structure") + "\n" + rows.join("\n");
    }

    function applyGeometry(data, planar) {
        if (!data || typeof data !== "object") return;
        if (data.minimizedMolecule) {
            var minimized = data.minimizedMolecule;
            state.xyzContent = moleculeToXyz(minimized, data.formula);
            state.pdbContent = "";
            state.molContent = "";
            state.moleculeName = minimized.name || data.formula || state.moleculeName;
            state.chemicalFormula = minimized.chemicalFormula || data.formula || state.chemicalFormula;
            state.vseprShape = minimized.vseprShape || "Relaxed conformer";
            state.idealBondAngleDegrees = minimized.idealBondAngleDegrees || 0;
            state.totalAtomCount = minimized.atoms ? minimized.atoms.length : state.totalAtomCount;
        } else if (planar) {
            state.planarPdbContent = data.pdbFormat || "";
            state.planarXyzContent = data.xyzFormat || "";
            state.planarMolContent = data.molFormat || "";
            state.skeletalSvgContent = data.skeletalSvg || data.svgContent || state.skeletalSvgContent;
        } else {
            state.pdbContent = data.pdbFormat || "";
            state.xyzContent = data.xyzFormat || "";
            state.molContent = data.molFormat || "";
            state.skeletalSvgContent = data.skeletalSvg || data.svgContent || state.skeletalSvgContent;
        }
        state.moleculeName = data.name || state.moleculeName;
        state.chemicalFormula = data.chemicalFormula || data.formula || state.chemicalFormula;
        state.molecularWeight = data.molecularWeight != null ? data.molecularWeight : state.molecularWeight;
        state.totalAtomCount = data.totalAtomCount != null ? data.totalAtomCount : state.totalAtomCount;
        state.vseprShape = data.vseprShape || state.vseprShape;
        state.idealBondAngleDegrees = data.idealBondAngleDegrees != null ? data.idealBondAngleDegrees : state.idealBondAngleDegrees;
        state.elementsPresent = data.elementsPresent || state.elementsPresent;
        state.functionalGroups = data.functionalGroups || state.functionalGroups;
        updateStudioMetadata();
        updateSkeletalView();
        renderViewer();
    }

    function renderGeometry(output, data, planar) {
        applyGeometry(data, planar);
        var title = data && data.minimizedMolecule ? "Energy-minimized structure" : (planar ? "Planar geometry" : "3D geometry");
        renderJson(output, data, title);
        var action = document.createElement("button");
        action.type = "button";
        action.className = "primary-button result-action";
        action.textContent = "Open in molecular studio";
        action.addEventListener("click", function () { showView("studio"); });
        output.insertBefore(action, output.querySelector(".result-details"));
    }

    async function executeApiAction(button) {
        var endpoint = button.dataset.endpoint;
        var payload = button.dataset.form ? serializeForm(button.dataset.form) : {};
        var pathInput = button.dataset.pathInput;
        if (pathInput) {
            var pathValue = payload[pathInput];
            if (pathValue == null || String(pathValue).trim() === "") {
                toast("Enter a value before running this action.", true);
                return;
            }
            endpoint = endpoint.replace("{" + pathInput + "}", encodeURIComponent(pathValue));
            delete payload[pathInput];
        }
        var method = button.dataset.method || ((pathInput || !button.dataset.form) ? "GET" : "POST");
        var output = byId(button.dataset.output);
        if (!output) return;
        var originalLabel = button.textContent;
        output.classList.add("is-loading");
        output.innerHTML = '<div class="loading-state">Running scientific calculation</div>';
        button.disabled = true;
        button.textContent = "Running…";
        try {
            var options = { method: method, headers: { "Accept": "application/json, image/svg+xml" } };
            if (method !== "GET") {
                options.headers["Content-Type"] = "application/json";
                options.body = JSON.stringify(payload);
            }
            var response = await fetch(endpoint, options);
            var contentType = response.headers.get("content-type") || "";
            var bodyText = await response.text();
            var data = bodyText;
            if (contentType.indexOf("json") >= 0 || /^[\[{]/.test(bodyText.trim())) {
                try { data = JSON.parse(bodyText); } catch (_) { data = bodyText; }
            }
            if (!response.ok) {
                var message = data && data.error ? data.error : ("Request failed with HTTP " + response.status + ".");
                throw new Error(message);
            }
            output.classList.remove("is-loading");
            var renderMode = button.dataset.render || "json";
            if (renderMode === "svg") renderSvg(output, data);
            else if (renderMode === "network") renderNetwork(output, data);
            else if (renderMode === "spectroscopy") renderSpectroscopy(output, data);
            else if (renderMode === "pubchem") renderPubChem(output, data);
            else if (renderMode === "geometry") renderGeometry(output, data, false);
            else if (renderMode === "geometry-planar") renderGeometry(output, data, true);
            else renderJson(output, data, originalLabel);
        } catch (error) {
            output.classList.remove("is-loading");
            output.innerHTML = '<div class="result-error"><strong>Calculation could not be completed.</strong><br />' + escapeHtml(error.message) + "</div>";
            toast(error.message, true);
        } finally {
            button.disabled = false;
            button.textContent = originalLabel;
        }
    }

    function initViewer() {
        if (!window.$3Dmol || !byId("viewer3d")) {
            byId("viewer3d").innerHTML = '<div class="viewer-unavailable">Interactive 3D rendering is unavailable. API calculations and structure exports remain available.</div>';
            return;
        }
        state.viewer = window.$3Dmol.createViewer("viewer3d", {
            backgroundColor: "#071524",
            antialias: true
        });
        renderViewer();
    }

    function viewerStyle() {
        var style = byId("renderStyleSelect").value;
        if (style === "stick") return { stick: { radius: 0.16, colorscheme: "Jmol" } };
        if (style === "sphere") return { sphere: { scale: 0.34, colorscheme: "Jmol" } };
        if (style === "wireframe") return { line: { linewidth: 2, colorscheme: "Jmol" } };
        return { stick: { radius: 0.13, colorscheme: "Jmol" }, sphere: { scale: 0.24, colorscheme: "Jmol" } };
    }

    function renderViewer() {
        if (!state.viewer) return;
        var planar = state.spatialMode === "flat";
        var pdb = planar ? state.planarPdbContent : state.pdbContent;
        var xyz = planar ? state.planarXyzContent : state.xyzContent;
        state.viewer.clear();
        if (pdb) state.viewer.addModel(pdb, "pdb", { keepH: true });
        else if (xyz) state.viewer.addModel(xyz, "xyz");
        else return;
        state.viewer.setStyle({}, viewerStyle());
        var surface = byId("surfaceSelect").value;
        if (surface !== "none") {
            var surfaceType = surface === "sas" ? window.$3Dmol.SurfaceType.SAS : (surface === "ses" ? window.$3Dmol.SurfaceType.SES : window.$3Dmol.SurfaceType.VDW);
            state.viewer.addSurface(surfaceType, { opacity: 0.52, color: "#a9c6ec" });
        }
        if (state.showLabels) {
            state.viewer.getModel().selectedAtoms({}).forEach(function (atom) {
                state.viewer.addLabel(atom.elem + (atom.serial || ""), {
                    position: atom,
                    fontColor: "#e8f2ff",
                    backgroundColor: "#0b1f36",
                    backgroundOpacity: 0.8,
                    fontSize: 10
                });
            });
        }
        state.viewer.zoomTo();
        state.viewer.render();
    }

    function updateSkeletalView() {
        var container = byId("skeletalSvgContainer");
        if (!container) return;
        var safe = sanitizeSvg(state.skeletalSvgContent);
        container.innerHTML = safe || '<div class="viewer-unavailable">No skeletal representation is available for this input.</div>';
    }

    function updateStudioMetadata() {
        byId("currentMoleculeName").textContent = state.moleculeName || state.currentInput;
        byId("currentFormulaBadge").textContent = state.chemicalFormula || state.currentInput;
        byId("metaHillFormula").textContent = state.chemicalFormula || "—";
        byId("metaMolarMass").textContent = formatValue(Number(state.molecularWeight || 0));
        byId("metaAtomCount").textContent = formatValue(state.totalAtomCount);
        byId("metaVseprShape").textContent = state.vseprShape || "—";
        byId("metaBondAngle").textContent = formatValue(Number(state.idealBondAngleDegrees || 0));
        byId("metaElements").textContent = state.elementsPresent && state.elementsPresent.length ? state.elementsPresent.join(" · ") : "—";
        var groups = byId("metaFunctionalGroups");
        groups.innerHTML = state.functionalGroups && state.functionalGroups.length
            ? state.functionalGroups.map(function (group) { return '<span class="data-tag">' + escapeHtml(group) + "</span>"; }).join("")
            : '<span class="data-tag">None identified</span>';
        document.querySelectorAll("[data-current-molecule]").forEach(function (input) { input.value = state.currentInput; });
    }

    async function requestJson(endpoint, payload) {
        var response = await fetch(endpoint, {
            method: "POST",
            headers: { "Content-Type": "application/json", "Accept": "application/json" },
            body: JSON.stringify(payload)
        });
        var data = await response.json().catch(function () { return {}; });
        if (!response.ok) throw new Error(data.error || ("Request failed with HTTP " + response.status + "."));
        return data;
    }

    async function loadMolecule(input, shape) {
        input = String(input || "").trim();
        if (!input) {
            toast("Enter a molecular formula, compound name, or bonded SMILES.", true);
            return;
        }
        var loadButton = byId("globalMoleculeForm").querySelector('button[type="submit"]');
        var original = loadButton.textContent;
        loadButton.disabled = true;
        loadButton.textContent = "Loading…";
        try {
            var payload = { formula: input, name: input };
            if (shape && shape !== "Auto") payload.overrideShape = shape;
            var results = await Promise.all([
                requestJson("/api/v1/geometry/3d", payload),
                requestJson("/api/v1/geometry/planar-3d", { formula: input, name: input })
            ]);
            state.currentInput = input;
            applyGeometry(results[0], false);
            applyGeometry(results[1], true);
            byId("globalMoleculeInput").value = input;
            toast((state.moleculeName || input) + " loaded into the molecular studio.");
            showView("studio");
        } catch (error) {
            toast(error.message, true);
        } finally {
            loadButton.disabled = false;
            loadButton.textContent = original;
        }
    }

    function downloadText(content, filename, mimeType) {
        if (!content) {
            toast("This export is not available for the current representation.", true);
            return;
        }
        var blob = new Blob([content], { type: mimeType || "text/plain;charset=utf-8" });
        var link = document.createElement("a");
        link.href = URL.createObjectURL(blob);
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        var objectUrl = link.href;
        link.remove();
        window.setTimeout(function () { URL.revokeObjectURL(objectUrl); }, 500);
    }

    function renderCompoundList(query) {
        var normalized = String(query || "").trim().toLowerCase();
        var filtered = compounds.filter(function (compound) {
            return !normalized || (compound.name + " " + compound.formula + " " + compound.group).toLowerCase().indexOf(normalized) >= 0;
        });
        byId("compoundCount").textContent = filtered.length;
        byId("compoundList").innerHTML = filtered.map(function (compound) {
            return '<button type="button" class="compound-item" data-compound-input="' + escapeHtml(compound.input) + '"><span><strong>' + escapeHtml(compound.name) + '</strong><small>' + escapeHtml(compound.formula) + " · " + escapeHtml(compound.group) + '</small></span><span>→</span></button>';
        }).join("");
        document.querySelectorAll("[data-compound-input]").forEach(function (button) {
            button.addEventListener("click", function () {
                byId("globalMoleculeInput").value = button.dataset.compoundInput;
                loadMolecule(button.dataset.compoundInput, "Auto");
            });
        });
    }

    function filterTools(query) {
        var normalized = String(query || "").trim().toLowerCase();
        document.querySelectorAll(".tool-group").forEach(function (group) {
            var visible = 0;
            group.querySelectorAll("[data-tool-target]").forEach(function (button) {
                var match = !normalized || button.textContent.toLowerCase().indexOf(normalized) >= 0 || group.dataset.toolGroup.toLowerCase().indexOf(normalized) >= 0;
                button.style.display = match ? "" : "none";
                if (match) visible += 1;
            });
            group.style.display = visible ? "" : "none";
        });
    }

    function loadDocument(key) {
        var content = bootstrap.documentation && bootstrap.documentation[key];
        var output = byId("documentContent");
        byId("documentTitle").textContent = documentTitles[key] || "Documentation";
        document.querySelectorAll("[data-doc-key]").forEach(function (button) {
            button.classList.toggle("is-active", button.dataset.docKey === key);
        });
        if (!content) {
            output.innerHTML = '<div class="result-error">This document is not available in the current build.</div>';
            return;
        }
        if (window.marked) {
            window.marked.setOptions({ gfm: true, breaks: false });
            output.innerHTML = window.marked.parse(content);
        } else {
            output.innerHTML = "<pre>" + escapeHtml(content) + "</pre>";
        }
        output.querySelectorAll("a").forEach(function (link) {
            var href = link.getAttribute("href") || "";
            var filename = href.split("/").pop().split("#")[0].toLowerCase();
            if (documentFileKeys[filename]) {
                link.addEventListener("click", function (event) {
                    event.preventDefault();
                    loadDocument(documentFileKeys[filename]);
                });
            } else if (/^https?:\/\//i.test(href)) {
                link.target = "_blank";
                link.rel = "noopener";
            }
        });
        output.scrollTop = 0;
    }

    async function checkHealth() {
        var indicator = byId("apiStatusIndicator");
        var statusText = byId("apiStatusText");
        var detail = byId("apiStatusDetail");
        try {
            var response = await fetch("/healthz", { headers: { "Accept": "application/json" }, cache: "no-store" });
            if (!response.ok) throw new Error("Service unavailable");
            var data = await response.json().catch(function () { return {}; });
            indicator.className = "status-dot is-online";
            statusText.textContent = "API operational";
            detail.textContent = data.status ? ("Chemy.Api · " + data.status) : "Chemy.Api · connected";
        } catch (_) {
            indicator.className = "status-dot is-offline";
            statusText.textContent = "API unavailable";
            detail.textContent = "Chemy.Api · retry available";
        }
    }

    function bindEvents() {
        document.querySelectorAll("[data-view-target]").forEach(function (button) {
            button.addEventListener("click", function () { showView(button.dataset.viewTarget); });
        });
        document.querySelectorAll("[data-open-tool]").forEach(function (button) {
            button.addEventListener("click", function () { activateTool(button.dataset.openTool); });
        });
        document.querySelectorAll("[data-tool-target]").forEach(function (button) {
            button.addEventListener("click", function () { activateTool(button.dataset.toolTarget); });
        });
        document.querySelectorAll("[data-api-action]").forEach(function (button) {
            button.addEventListener("click", function () { executeApiAction(button); });
        });
        document.querySelectorAll(".tool-form").forEach(function (form) {
            form.addEventListener("submit", function (event) {
                event.preventDefault();
                var primary = form.querySelector("[data-api-action]");
                if (primary) executeApiAction(primary);
            });
        });
        document.querySelectorAll("[data-doc-key]").forEach(function (button) {
            button.addEventListener("click", function () { loadDocument(button.dataset.docKey); });
        });
        byId("globalMoleculeForm").addEventListener("submit", function (event) {
            event.preventDefault();
            loadMolecule(byId("globalMoleculeInput").value, byId("globalShapeInput").value);
        });
        byId("toolSearchInput").addEventListener("input", function (event) { filterTools(event.target.value); });
        byId("compoundSearchInput").addEventListener("input", function (event) { renderCompoundList(event.target.value); });
        byId("codeSearchInput").addEventListener("input", function (event) {
            var query = event.target.value.trim().toLowerCase();
            document.querySelectorAll("[data-code-search]").forEach(function (card) {
                var match = !query || card.dataset.codeSearch.indexOf(query) >= 0 || card.textContent.toLowerCase().indexOf(query) >= 0;
                card.style.display = match ? "" : "none";
            });
        });
        byId("mobileMenuButton").addEventListener("click", function () { document.body.classList.toggle("sidebar-open"); });
        byId("sidebarScrim").addEventListener("click", function () { document.body.classList.remove("sidebar-open"); });
        byId("refreshHealthButton").addEventListener("click", checkHealth);
        byId("themeToggleButton").addEventListener("click", toggleTheme);
        byId("scrollDocumentTop").addEventListener("click", function () { byId("documentContent").scrollTo({ top: 0, behavior: "smooth" }); });
        document.querySelectorAll("[data-visual-mode]").forEach(function (button) {
            button.addEventListener("click", function () {
                state.visualMode = button.dataset.visualMode;
                document.querySelectorAll("[data-visual-mode]").forEach(function (item) { item.classList.toggle("is-active", item === button); });
                byId("viewer3d").classList.toggle("is-hidden", state.visualMode !== "3d");
                byId("viewer2d").classList.toggle("is-hidden", state.visualMode !== "2d");
                byId("viewer3dControls").classList.toggle("is-hidden", state.visualMode !== "3d");
                byId("viewer2dControls").classList.toggle("is-hidden", state.visualMode !== "2d");
                if (state.visualMode === "3d" && state.viewer) {
                    state.viewer.resize();
                    state.viewer.render();
                }
            });
        });
        byId("spatialModeSelect").addEventListener("change", function (event) {
            state.spatialMode = event.target.value;
            renderViewer();
        });
        byId("renderStyleSelect").addEventListener("change", renderViewer);
        byId("surfaceSelect").addEventListener("change", renderViewer);
        byId("labelsViewerButton").addEventListener("click", function () {
            state.showLabels = !state.showLabels;
            byId("labelsViewerButton").classList.toggle("is-active", state.showLabels);
            renderViewer();
        });
        byId("fitViewerButton").addEventListener("click", function () {
            if (state.viewer) { state.viewer.zoomTo(); state.viewer.render(); }
        });
        byId("spinViewerButton").addEventListener("click", function () {
            if (!state.viewer) return;
            state.spinning = !state.spinning;
            state.viewer.spin(state.spinning ? "y" : false);
            byId("spinViewerButton").textContent = state.spinning ? "Stop rotation" : "Rotate";
        });
        document.querySelectorAll("[data-download-format]").forEach(function (button) {
            button.addEventListener("click", function () {
                var format = button.dataset.downloadFormat;
                var planar = state.spatialMode === "flat";
                var content = format === "mol" ? (planar ? state.planarMolContent : state.molContent) : (format === "pdb" ? (planar ? state.planarPdbContent : state.pdbContent) : (planar ? state.planarXyzContent : state.xyzContent));
                var base = (state.chemicalFormula || "chemy-structure").replace(/[^a-z0-9_-]+/gi, "-");
                downloadText(content, base + "." + format, "chemical/x-" + format);
            });
        });
        byId("downloadSkeletalButton").addEventListener("click", function () {
            downloadText(state.skeletalSvgContent, (state.chemicalFormula || "chemy-structure") + ".svg", "image/svg+xml");
        });
        window.addEventListener("resize", function () {
            if (state.viewer && state.visualMode === "3d") {
                state.viewer.resize();
                state.viewer.render();
            }
        });
    }

    function initialize() {
        bindEvents();
        syncThemeControl();
        renderCompoundList("");
        updateStudioMetadata();
        updateSkeletalView();
        initViewer();
        loadDocument("home");
        checkHealth();
        window.setInterval(checkHealth, 30000);
        if (bootstrap.errorMessage) toast(bootstrap.errorMessage, true);
    }

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", initialize);
    else initialize();
})();
