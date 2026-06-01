using UnityEngine;
using StarterAssets;

public class ZonaInteraccion : MonoBehaviour
{
    private PlayerRole _jugadorDentro = null;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerRole role))
        {
            _jugadorDentro = role;
            Debug.Log($"✅ Jugador entró a zona: {role.MyRole}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerRole role))
        {
            if (_jugadorDentro == role)
            {
                _jugadorDentro = null;
                Debug.Log("🚪 Jugador salió de zona");
            }
        }
    }

    private void Update()
    {
        // Limpiamos si el jugador se desconectó
        if (_jugadorDentro != null && _jugadorDentro.gameObject == null)
        {
            _jugadorDentro = null;
            return;
        }

        if (_jugadorDentro == null) return;

        // --- EL ARREGLO MÁGICO ESTÁ AQUÍ ---
        // Verificamos si este personaje te pertenece a ti (ya seas Host o Cliente)
        bool esMiJugador = _jugadorDentro.Object.HasInputAuthority || _jugadorDentro.Object.HasStateAuthority;
        if (!esMiJugador) return;

        var input = _jugadorDentro.GetComponent<StarterAssetsInputs>();
        if (input == null || !input.interact) return;

        // Consumimos el input
        input.interact = false;

        Debug.Log("⌨️ ¡E presionada en zona y enviando orden al servidor!");
        _jugadorDentro.InteractuarCon(gameObject);
    }

    private void OnDisable()
    {
        _jugadorDentro = null;
    }
}