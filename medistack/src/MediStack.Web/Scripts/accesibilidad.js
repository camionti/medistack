(function () {
    "use strict";

    const CLAVE = "medistack.ajustes-visuales.v1";
    const TAMANOS = ["normal", "grande", "muy-grande"];
    const raiz = document.documentElement;
    let puedeGuardar = true;

    function valoresIniciales() {
        return { tamano: "normal", contraste: false };
    }

    function validar(valor) {
        if (!valor || typeof valor !== "object") {
            return valoresIniciales();
        }
        return {
            tamano: TAMANOS.includes(valor.tamano) ? valor.tamano : "normal",
            contraste: valor.contraste === true
        };
    }

    function leer() {
        let texto;
        try {
            texto = window.localStorage.getItem(CLAVE);
        } catch (error) {
            puedeGuardar = false;
            return valoresIniciales();
        }
        try {
            return validar(texto ? JSON.parse(texto) : null);
        } catch (error) {
            return valoresIniciales();
        }
    }

    function aplicar(preferencias) {
        raiz.setAttribute("data-ms-tamano", preferencias.tamano);
        raiz.setAttribute("data-ms-contraste", preferencias.contraste ? "alto" : "normal");
    }
    let preferencias = leer();
    aplicar(preferencias);

    function iniciarControles() {
        const panel = document.getElementById("ms-ajustes-visuales");
        const resumen = document.getElementById("ms-ajustes-resumen");
        const tamano = document.getElementById("ms-tamano-texto");
        const contraste = document.getElementById("ms-alto-contraste");
        const restablecer = document.getElementById("ms-restablecer");
        const estado = document.getElementById("ms-ajustes-estado");
        const opciones = document.getElementById("ms-ajustes-opciones");
        const cerrar = document.getElementById("ms-ajustes-cerrar");

        if (!panel || !resumen || !tamano || !contraste || !restablecer || !estado || !opciones || !cerrar) {
            return;
        }

        function sincronizar() {
            tamano.value = preferencias.tamano;
            contraste.checked = preferencias.contraste;
        }

        function informar(mensaje) {
            estado.textContent = mensaje + (puedeGuardar
                ? " Las preferencias se conservan en este navegador."
                : " El navegador no permite guardarlas: el cambio solo se conserva en esta página.");
        }

        function guardar(esRestablecimiento) {
            try {
                if (esRestablecimiento) {
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
            preferencias = validar({ tamano: tamano.value, contraste: contraste.checked });
            aplicar(preferencias);
            guardar(false);
            informar("Texto: " + tamano.options[tamano.selectedIndex].text + ". Alto contraste "
                + (preferencias.contraste ? "activado." : "desactivado."));
        }

        opciones.disabled = false;
        sincronizar();
        informar("Ajustes listos.");
        tamano.addEventListener("change", cambiar);
        contraste.addEventListener("change", cambiar);

        restablecer.addEventListener("click", function () {
            preferencias = valoresIniciales();
            aplicar(preferencias);
            guardar(true);
            sincronizar();
            informar("Se restableció la apariencia original.");
        });

        function cerrarPanel() {
            panel.open = false;
            resumen.focus();
        }

        cerrar.addEventListener("click", cerrarPanel);

        panel.addEventListener("toggle", function () {
            resumen.setAttribute("aria-label", panel.open
                ? "Cerrar ajustes de accesibilidad"
                : "Abrir ajustes de accesibilidad");
        });

        document.addEventListener("pointerdown", function (evento) {
            if (panel.open && !panel.contains(evento.target)) {
                panel.open = false;
            }
        });
        panel.addEventListener("keydown", function (evento) {
            if (evento.key === "Escape" && panel.open) {
                cerrarPanel();
                evento.preventDefault();
            }
        });
        window.addEventListener("storage", function (evento) {
            if (evento.key !== CLAVE && evento.key !== null) {
                return;
            }
            preferencias = leer();
            aplicar(preferencias);
            sincronizar();
            informar("Se actualizaron los ajustes desde otra pestaña.");
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", iniciarControles, { once: true });
    } else {
        iniciarControles();
    }
}());
