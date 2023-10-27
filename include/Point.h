#ifndef POINT_H
#define POINT_H

#include "pugixml.hpp"

class Point {
private:
	pugi::xml_node root;
	double x, y;
public:
	Point() noexcept;
	Point(pugi::xml_node& pointNode) noexcept;
	Point(const pugi::xml_node& pointNode) noexcept;
	~Point() noexcept;
	Point(const Point& point) noexcept;
	double getX() const noexcept;
	void setX(double x) noexcept;
	double getY() const noexcept;
	void setY(double y) noexcept;
	pugi::xml_node& getRoot() noexcept;
	const pugi::xml_node& getRoot() const noexcept;
};

#endif // POINT_H