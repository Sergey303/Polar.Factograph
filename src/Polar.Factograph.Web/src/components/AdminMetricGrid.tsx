interface AdminMetric {
  label: string;
  value: string | number;
}

interface AdminMetricGridProps {
  items: AdminMetric[];
}

export function AdminMetricGrid({ items }: AdminMetricGridProps) {
  return (
    <dl className="admin-metric-grid">
      {items.map(item => (
        <div key={item.label} className="admin-metric">
          <dt>{item.label}</dt>
          <dd>{item.value}</dd>
        </div>
      ))}
    </dl>
  );
}
