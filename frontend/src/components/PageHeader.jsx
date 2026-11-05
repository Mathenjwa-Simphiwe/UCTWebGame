export default function PageHeader({ eyebrow, title, children, actions }) {
  return <section className="page-head"><div><span className="eyebrow">{eyebrow}</span><h1>{title}</h1>{children && <p>{children}</p>}</div>{actions && <div className="head-actions">{actions}</div>}</section>
}
