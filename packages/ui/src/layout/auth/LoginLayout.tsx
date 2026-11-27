import { useId, type ReactNode } from 'react';
import { BrandMark } from '../../components/brand/BrandMark';
import { LanguageSegmented } from '../../components/navigation/LanguageSegmented';
import { ProductPreview, type ProductPreviewProps } from './ProductPreview';

export interface LoginFeature {
  icon: ReactNode;
  title: string;
  description: string;
}

export interface LoginLayoutProps {
  appName: string;
  announcement?: string;
  headline: string;
  description: string;
  features: LoginFeature[];
  preview?: ProductPreviewProps;
  footerNote: string;
  footerExtra?: ReactNode;
  children: ReactNode;
}

function Grain() {
  const id = useId();
  return (
    <svg className="erp-login__grain" aria-hidden="true">
      <filter id={id}>
        <feTurbulence
          type="fractalNoise"
          baseFrequency="0.85"
          numOctaves="3"
          stitchTiles="stitch"
        />
      </filter>
      <rect width="100%" height="100%" filter={`url(#${id})`} />
    </svg>
  );
}

export function LoginLayout({
  appName,
  announcement,
  headline,
  description,
  features,
  preview,
  footerNote,
  footerExtra,
  children,
}: LoginLayoutProps) {
  return (
    <div className="erp-login">
      <section className="erp-login__form">
        <header className="erp-login__head">
          <span className="erp-login__brand">
            <BrandMark size={34} label={appName} />
            <span className="erp-login__brandname">{appName}</span>
          </span>
          <LanguageSegmented />
        </header>
        <div className="erp-login__body">
          <div className="erp-login__inner">{children}</div>
        </div>
        <footer className="erp-login__foot">
          <span>{footerNote}</span>
          {footerExtra}
        </footer>
      </section>

      <section className="erp-login__brandpanel" aria-label={headline}>
        <span className="erp-login__blob is-a" aria-hidden="true" />
        <span className="erp-login__blob is-b" aria-hidden="true" />
        <Grain />

        <div className="erp-login__copy">
          {announcement && (
            <span className="erp-login__pill">
              <span className="erp-login__pilldot" />
              {announcement}
            </span>
          )}
          <h2>{headline}</h2>
          <p>{description}</p>
        </div>

        {preview && (
          <div className="erp-login__preview">
            <div className="erp-login__frame">
              <ProductPreview {...preview} />
            </div>
          </div>
        )}

        <div className="erp-login__features">
          {features.map((feature) => (
            <div key={feature.title} className="erp-login__feature">
              <span className="erp-login__featicon">{feature.icon}</span>
              <span className="erp-login__feattext">
                <span className="erp-login__feattitle">{feature.title}</span>
                <span className="erp-login__featdesc">{feature.description}</span>
              </span>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}
