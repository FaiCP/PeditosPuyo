import { MapContainer, Marker, TileLayer, useMapEvents } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import type { Coords } from '../../utils/coords';

const pinIcon = L.divIcon({
  className: 'custom-map-pin',
  html: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="#ef4444" width="28" height="38"><path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zM7 9c0-2.76 2.24-5 5-5s5 2.24 5 5-2.24 5-5 5-5-2.24-5-5z"/></svg>`,
  iconSize: [28, 38],
  iconAnchor: [14, 38],
});

interface ClickHandlerProps {
  onChange: (c: Coords) => void;
}

function ClickHandler({ onChange }: ClickHandlerProps) {
  useMapEvents({
    click(e) {
      onChange({ lat: e.latlng.lat, lng: e.latlng.lng });
    },
  });
  return null;
}

interface MapPickerProps {
  value: Coords | null;
  onChange: (c: Coords) => void;
  center?: Coords;
  height?: string;
}

const DEFAULT_CENTER: Coords = { lat: -1.492, lng: -78.002 };

export default function MapPicker({ value, onChange, center, height = '260px' }: MapPickerProps) {
  const initialCenter = center ?? value ?? DEFAULT_CENTER;

  return (
    <div style={{ height }} className="rounded-xl overflow-hidden border border-[var(--card-edge)]">
      <MapContainer
        center={[initialCenter.lat, initialCenter.lng]}
        zoom={15}
        style={{ height: '100%', width: '100%' }}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <ClickHandler onChange={onChange} />
        {value && <Marker position={[value.lat, value.lng]} icon={pinIcon} />}
      </MapContainer>
    </div>
  );
}
