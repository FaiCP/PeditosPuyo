import { useState } from 'react';
import { coordsFromInput, currentLocation, mapsUrl, type Coords } from '../../utils/coords';
import MapPicker from './MapPicker';

interface Props {
  label: string;
  value: Coords | null;
  onChange: (c: Coords | null, rawLink: string) => void;
  linkValue?: string;
  hint?: string;
}

export default function LocationField({ label, value, onChange, linkValue = '', hint }: Props) {
  const [link, setLink] = useState(linkValue);
  const [msg, setMsg] = useState('');
  const [busy, setBusy] = useState(false);

  const detect = async () => {
    setBusy(true);
    setMsg('');
    try {
      const c = await coordsFromInput(link);
      onChange(c, link);
      setMsg('');
    } catch (e) {
      setMsg((e as Error).message);
    } finally {
      setBusy(false);
    }
  };

  const useMy = async () => {
    setBusy(true);
    setMsg('');
    try {
      const c = await currentLocation();
      onChange(c, link);
      setLink(`${c.lat.toFixed(6)}, ${c.lng.toFixed(6)}`);
      setMsg('');
    } catch (e) {
      setMsg((e as Error).message);
    } finally {
      setBusy(false);
    }
  };

  const handleMapChange = (c: Coords) => {
    const raw = `${c.lat.toFixed(6)}, ${c.lng.toFixed(6)}`;
    setLink(raw);
    setMsg('');
    onChange(c, raw);
  };

  return (
    <div>
      <label className="cust-label">{label}</label>
      <div className="flex gap-2">
        <input
          type="text"
          className="cust-input flex-1"
          value={link}
          onChange={(e) => {
            setLink(e.target.value);
            setMsg('');
          }}
          placeholder="Pega el link de Google Maps, «lat, lng» o marca en el mapa"
        />
        <button type="button" className="cust-btn cust-btn-ghost whitespace-nowrap" onClick={detect} disabled={busy}>
          Detectar
        </button>
      </div>

      <div className="mt-3">
        <MapPicker value={value} onChange={handleMapChange} />
        <p className="text-[#7d8a7f] text-xs mt-2">
          Toca el mapa para marcar el punto exacto. También puedes pegar un link de Google Maps arriba.
        </p>
      </div>

      <div className="flex items-center gap-3 mt-3 text-sm">
        <button type="button" onClick={useMy} disabled={busy} className="text-[var(--leaf)] font-semibold hover:underline disabled:opacity-50">
          📍 Usar mi ubicación actual
        </button>
        {value && (
          <a href={mapsUrl(value)} target="_blank" rel="noreferrer" className="text-[var(--gold)] underline underline-offset-2">
            Ver en mapa ✓
          </a>
        )}
      </div>
      {msg && <p className="text-[var(--danger)] text-sm mt-2">{msg}</p>}
      {hint && !msg && <p className="text-[#7d8a7f] text-xs mt-2">{hint}</p>}
    </div>
  );
}
