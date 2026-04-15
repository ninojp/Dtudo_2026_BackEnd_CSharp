import imgLogoHardware from './logo-hardware.webp';

export default function LogoHardware({largura, altura}) {
  return (
    <img src={imgLogoHardware} alt='Logo Hardware' style={{width: largura, height: altura}} />
  );
};
