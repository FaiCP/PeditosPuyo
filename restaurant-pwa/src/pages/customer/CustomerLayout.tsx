import { Link, Outlet, useParams } from 'react-router-dom';
import './customer-theme.css';

const TICKER = 'Pide de tu restaurante favorito · Mandados y compras · Pagas al recibir · ';

export default function CustomerLayout() {
  const { token = '' } = useParams();
  const home = `/p/${token}`;

  return (
    <div className="cust">
      <div className="cust-ticker">
        <span>{TICKER.repeat(4)}</span>
      </div>
      <header className="max-w-3xl mx-auto px-4 pt-5 pb-2 flex items-center justify-between">
        <Link to={home} className="cust-display text-2xl" style={{ color: 'var(--cream)' }}>
          Peditos<span style={{ color: 'var(--tang)' }}>.</span>Puyo
        </Link>
        <Link
          to={`${home}/mis-pedidos`}
          className="text-sm font-semibold px-3 py-2 rounded-full border"
          style={{ borderColor: 'var(--card-edge)', color: 'var(--cream-dim)' }}
        >
          Mis pedidos
        </Link>
      </header>
      <main className="max-w-3xl mx-auto px-4 pb-24">
        <Outlet />
      </main>
    </div>
  );
}
