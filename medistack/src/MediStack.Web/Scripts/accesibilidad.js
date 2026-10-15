(function () {
    "use strict";

    const CLAVE = "medistack.ajustes-visuales.v1";
    const raiz = document.documentElement;
    let puedeGuardar = true;

    function numero(valor, defecto, minimo, maximo, paso) {
        const n = Number(valor);
        if (!Number.isFinite(n)) {
            return defecto;
        }
        return Math.min(maximo, Math.max(minimo, Math.round(n / paso) * paso));
    }

    function iniciales() {
        return {
            tamano: 100,
            fuente: "original",
            interlineado: 1.5,
            letras: 0,
            ancho: "normal",
            negrita: false,
            contraste: false,
            enlaces: false,
            movimiento: false
        };
    }

    function validar(valor) {
        if (!valor || typeof valor !== "object") {
            return iniciales();
        }
        const anterior = { normal: 100, grande: 150, "muy-grande": 200 };
        const tamano = Object.prototype.hasOwnProperty.call(anterior, valor.tamano)
            ? anterior[valor.tamano] : valor.tamano;
        return {
            tamano: numero(tamano, 100, 100, 200, 10),
            fuente: ["original", "sans", "serif"].includes(valor.fuente) ? valor.fuente : "original",
            interlineado: numero(valor.interlineado !== undefined ? valor.interlineado : (valor.espaciado ? 1.8 : 1.5), 1.5, 1.5, 2, 0.1),
            letras: numero(valor.letras !== undefined ? valor.letras : (valor.espaciado ? 0.5 : 0), 0, 0, 2, 0.5),
            ancho: ["normal", "estrecho", "amplio"].includes(valor.ancho) ? valor.ancho : "normal",
            negrita: valor.negrita === true,
            contraste: valor.contraste === true,
            enlaces: valor.enlaces === true,
            movimiento: valor.movimiento === true
        };
    }

    function leer() {
        try {
            const texto = window.localStorage.getItem(CLAVE);
            if (!texto) {
                return iniciales();
            }
            try {
                return validar(JSON.parse(texto));
            } catch (error) {
                return iniciales();
            }
        } catch (error) {
            puedeGuardar = false;
            return iniciales();
        }
    }

    function aplicar(valor) {
        raiz.setAttribute("data-ms-tamano", valor.tamano === 100 ? "normal" : valor.tamano < 200 ? "grande" : "muy-grande");
        raiz.style.setProperty("--ms-tamano-texto", valor.tamano + "%");
        raiz.setAttribute("data-ms-fuente", valor.fuente);
        raiz.setAttribute("data-ms-interlineado", valor.interlineado > 1.5 ? "si" : "no");
        raiz.style.setProperty("--ms-interlineado", valor.interlineado.toFixed(1));
        raiz.setAttribute("data-ms-letras", valor.letras > 0 ? "si" : "no");
        raiz.style.setProperty("--ms-letras", valor.letras + "px");
        raiz.setAttribute("data-ms-ancho", valor.ancho);
        raiz.setAttribute("data-ms-negrita", valor.negrita ? "si" : "no");
        raiz.setAttribute("data-ms-contraste", valor.contraste ? "alto" : "normal");
        raiz.setAttribute("data-ms-enlaces", valor.enlaces ? "si" : "no");
        raiz.setAttribute("data-ms-movimiento", valor.movimiento ? "reducido" : "normal");
    }

    let preferencias = leer();
    aplicar(preferencias);

    function iniciar() {
        const ids = {
            panel: "ms-ajustes-visuales", resumen: "ms-ajustes-resumen", cerrar: "ms-ajustes-cerrar",
            opciones: "ms-ajustes-opciones", estado: "ms-ajustes-estado", restablecer: "ms-restablecer",
            tamano: "ms-tamano-texto", tamanoValor: "ms-tamano-valor", fuente: "ms-fuente",
            interlineado: "ms-interlineado", interlineadoValor: "ms-interlineado-valor",
            letras: "ms-espaciado-letras", letrasValor: "ms-letras-valor", ancho: "ms-ancho-parrafo",
            negrita: "ms-texto-negrita", contraste: "ms-alto-contraste",
            enlaces: "ms-resaltar-enlaces", movimiento: "ms-reducir-movimiento"
        };
        const el = {};
        for (const clave of Object.keys(ids)) {
            el[clave] = document.getElementById(ids[clave]);
            if (!el[clave]) {
                return;
            }
        }

        function sincronizar() {
            el.tamano.value = String(preferencias.tamano);
            el.tamanoValor.textContent = preferencias.tamano + " %";
            el.fuente.value = preferencias.fuente;
            el.interlineado.value = preferencias.interlineado.toFixed(1);
            el.interlineadoValor.textContent = preferencias.interlineado.toFixed(1).replace(".", ",");
            el.letras.value = String(preferencias.letras);
            el.letrasValor.textContent = String(preferencias.letras).replace(".", ",") + " px";
            el.ancho.value = preferencias.ancho;
            el.negrita.checked = preferencias.negrita;
            el.contraste.checked = preferencias.contraste;
            el.enlaces.checked = preferencias.enlaces;
            el.movimiento.checked = preferencias.movimiento;
        }

        function informar(mensaje) {
            el.estado.textContent = mensaje + (puedeGuardar
                ? " Los ajustes se conservan en este navegador."
                : " Este navegador no permitió guardar las preferencias.");
        }

        function guardar(restablecer) {
            try {
                if (restablecer) {
                    window.localStorage.removeItem(CLAVE);
                } else {
                    window.localStorage.setItem(CLAVE, JSON.stringify(preferencias));
                }
                puedeGuardar = true;
            } catch (error) {
                puedeGuardar = false;
            }
        }

        function cambiar() {
            preferencias = validar({
                tamano: el.tamano.value,
                fuente: el.fuente.value,
                interlineado: el.interlineado.value,
                letras: el.letras.value,
                ancho: el.ancho.value,
                negrita: el.negrita.checked,
                contraste: el.contraste.checked,
                enlaces: el.enlaces.checked,
                movimiento: el.movimiento.checked
            });
            aplicar(preferencias);
            sincronizar();
            guardar(false);
            informar("Se aplicaron los ajustes.");
        }

        el.opciones.disabled = false;
        sincronizar();
        informar("Ajustes listos.");
        for (const control of [el.tamano, el.fuente, el.interlineado, el.letras, el.ancho, el.negrita, el.contraste, el.enlaces, el.movimiento]) {
            control.addEventListener("input", cambiar);
            if (control.tagName === "SELECT" || control.type === "checkbox") {
                control.addEventListener("change", cambiar);
            }
        }

        el.restablecer.addEventListener("click", function () {
            preferencias = iniciales();
            aplicar(preferencias);
            guardar(true);
            sincronizar();
            informar("Se restableció la apariencia original.");
        });

        function cerrar() {
            el.panel.open = false;
            el.resumen.focus();
        }

        el.cerrar.addEventListener("click", cerrar);
        el.panel.addEventListener("toggle", function () {
            el.resumen.setAttribute("aria-label", el.panel.open ? "Cerrar ajustes de accesibilidad" : "Abrir ajustes de accesibilidad");
        });
        el.panel.addEventListener("keydown", function (evento) {
            if (evento.key === "Escape" && el.panel.open) {
                evento.preventDefault();
                cerrar();
            }
        });
        document.addEventListener("pointerdown", function (evento) {
            if (el.panel.open && !el.panel.contains(evento.target)) {
                el.panel.open = false;
            }
        });
        window.addEventListener("storage", function (evento) {
            if (evento.key !== CLAVE && evento.key !== null) {
                return;
            }
            preferencias = leer();
            aplicar(preferencias);
            sincronizar();
            informar("Los ajustes se actualizaron desde otra pestaña.");
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", iniciar, { once: true });
    } else {
        iniciar();
    }
}());
